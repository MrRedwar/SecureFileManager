using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SecureFileManager.Wpf;
using SecureFileManager.Wpf.ViewModels;
using SecureFileManager.Wpf.Views;
using SecureFileManager.Core.Interfaces;
using SecureFileManager.Core.Results;
using SecureFileManager.Core.Files;
using Microsoft.Extensions.DependencyInjection;
using SecureFileManager.Wpf.Infrastructure.FileSystem;

namespace SecureFileManager.Tests;

[CollectionDefinition("WPF shell", DisableParallelization = true)]
public sealed class WpfShellCollection;

[Collection("WPF shell")]
public sealed class WpfShellTests
{
    [Fact]
    public void AllScreensResourcesAndLayoutsRenderWithoutBindingErrors()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { CheckShell(); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(60)), "WPF layout check timed out.");
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static void CheckShell()
    {
        var app = new App();
        app.InitializeComponent();
        var trace = new StringWriter();
        var listener = new TextWriterTraceListener(trace);
        PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
        // UI fixture only: protected shell layout, no real account/data or authentication bypass in App.
        var model = new MainWindowViewModel(new(), new(), new(), new(), new(), new(), new RenderSession());
        var window = new MainWindow(model);
        window.ShowActivated = false;
        window.Left = -20000;
        window.Top = -20000;
        // This test measures WPF client layouts; native window management is accepted manually.
        var root = (FrameworkElement)window.Content;
        Assert.Equal(3, window.CommandBindings.Count);
        var export = Environment.GetEnvironmentVariable("SFM_UI_ARTIFACT_DIRECTORY");
        if (export is not null) Directory.CreateDirectory(export);
        var expectedTypes = new[] { typeof(DrivesView), typeof(FilesView), typeof(ArchivesView),
            typeof(FormatsView), typeof(JournalView), typeof(SettingsView) };
        try
        {
            // Resolve the actual application graph while replacing only the local disk root, never connecting to SQL.
            using (var graph = App.CreateServices().AddSingleton(new PersonalStorage(Path.Combine(Path.GetTempPath(), "sfm-ui-graph-" + Guid.NewGuid().ToString("N"))))
                .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }))
            {
                var login = graph.GetRequiredService<AuthenticationWindow>();
                var shell = graph.GetRequiredService<MainWindow>();
                Assert.NotNull(graph.GetRequiredService<Core.Files.IFileService>());
                Assert.NotNull(graph.GetRequiredService<IFileDialogs>());
                login.Close(); shell.Close();
            }
            foreach (var key in new[] { "Icon.Eye", "Icon.Drives", "Icon.Files", "Icon.Archives", "Icon.JsonXml",
                "Icon.Journal", "Icon.Settings", "Icon.Create", "Icon.Delete", "Icon.Search", "Icon.Security",
                "Icon.User", "Icon.Database", "Art.Anubis", "Art.Header", "Art.Footer", "Texture.Papyrus", "Texture.Stone" })
            {
                var bitmap = Assert.IsAssignableFrom<BitmapSource>(app.FindResource(key));
                Assert.True(bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0, key);
            }
            // Instantiate the real drive item template: an empty list cannot expose invalid Value bindings.
            var drivesModel = (DrivesViewModel)model.Sections[0];
            drivesModel.Items.Add(new(new("TEST-ready", "UI fixture", "Fixed", "NTFS", true, 1000, 250)));
            drivesModel.Items.Add(new(new("TEST-unready", "", "Removable", "", false, 0, 0)));
            for (var index = 0; index < model.Sections.Count; index++)
            {
                var section = model.Sections[index];
                Assert.True(model.NavigateCommand.CanExecute(section));
                model.NavigateCommand.Execute(section);
                Assert.Same(section, model.SelectedSection);
                Assert.False(section.UnavailableCommand.CanExecute(null));
                foreach (var size in new[] { new Size(1440, 900), new Size(1024, 640) })
                {
                    window.Width = size.Width;
                    window.Height = size.Height;
                    // Render a deterministic client layout; late native WM_SIZE must not restore the previous size.
                    root.Width = size.Width;
                    root.Height = size.Height;
                    root.InvalidateMeasure();
                    root.Measure(size);
                    root.Arrange(new Rect(size));
                    root.UpdateLayout();
                    root.UpdateLayout();
                    Assert.Contains(Descendants(root), visual => visual.GetType() == expectedTypes[index]);
                    if (section is DrivesViewModel)
                    {
                        var capacityBars = Descendants(root).OfType<ProgressBar>()
                            .Where(bar => bar.DataContext is DriveCard).ToArray();
                        Assert.Equal(2, capacityBars.Length);
                        Assert.Equal(75d, capacityBars[0].Value);
                        Assert.Equal(0d, capacityBars[1].Value);
                        Assert.All(capacityBars, bar => Assert.Equal(BindingMode.OneWay,
                            BindingOperations.GetBinding(bar, ProgressBar.ValueProperty)!.Mode));
                    }
                    var navigation = Descendants(root).OfType<ListBox>().Single(x => AutomationProperties.GetName(x) == "Разделы приложения");
                    Assert.Same(section, navigation.SelectedItem);
                    Assert.True(root.ActualWidth <= size.Width && root.ActualHeight <= size.Height,
                        $"Actual layout {root.ActualWidth:R}x{root.ActualHeight:R}; target {size.Width}x{size.Height}");
                    foreach (var grid in Descendants(root).OfType<DataGrid>())
                    {
                        Assert.Empty(grid.Items);
                        Assert.True(grid.ActualWidth > 0);
                    }
                    if (export is not null)
                        Export(root, size, 1, Path.Combine(export, $"{index + 1:00}-{size.Width}x{size.Height}-render.png"));
                    if (section is FilesViewModel && export is not null)
                        Export(root, size, 1.5, Path.Combine(export, $"files-{size.Width}x{size.Height}-150pct-render.png"));
                }
            }
            Assert.False(model.NavigateCommand.CanExecute(new SectionViewModel("Unknown", "")));
            Assert.False(model.NavigateCommand.CanExecute(null));
            model.NavigateCommand.Execute(model.Sections[1]);
            model.SelectedSection.IsLoading = true;
            root.UpdateLayout();
            Assert.Contains(Descendants(root).OfType<ProgressBar>(), bar => bar.IsIndeterminate);
            Assert.Contains(Descendants(root).OfType<ProgressBar>(), bar => bar.Visibility == Visibility.Visible);
            model.SelectedSection.IsLoading = false;
            model.SelectedSection.ErrorMessage = "Тест состояния: данные недоступны";
            root.UpdateLayout();
            Assert.Contains(Descendants(root).OfType<TextBlock>(), text => text.Text == "Тест состояния: данные недоступны");
            model.SelectedSection.ErrorMessage = null;
            var filesModel = (FilesViewModel)model.Sections[1];
            filesModel.Items.Add(new("TEST-high.txt", "TXT", "20 Б", "—", "Icon.Files")
                { Entry = new(Guid.NewGuid(), "TEST-high.txt", "TEST-high.txt", Core.Entities.FileKind.File, 20, DateTime.UtcNow, Core.Files.EntryState.Managed) });
            filesModel.Items.Add(new("TEST-low.txt", "TXT", "2 Б", "—", "Icon.Files")
                { Entry = new(Guid.NewGuid(), "TEST-low.txt", "TEST-low.txt", Core.Entities.FileKind.File, 2, DateTime.UtcNow, Core.Files.EntryState.Managed) });
            root.UpdateLayout();
            if (export is not null) Export(root, new Size(1024, 640), 1, Path.Combine(export, "files-TEST-fixture-render.png"));
            filesModel.SearchText = "low";
            Assert.Single(filesModel.View.Cast<FileListItem>());
            filesModel.SearchText = ""; filesModel.SortIndex = 2;
            Assert.Equal("TEST-low.txt", filesModel.View.Cast<FileListItem>().First().Name);
            filesModel.Descending = true;
            Assert.Equal("TEST-high.txt", filesModel.View.Cast<FileListItem>().First().Name);
            filesModel.ResetUserState();
            Assert.Empty(filesModel.Items); Assert.Empty(filesModel.SearchText);
            CheckFileDialogsAndImport();
            CheckAuthentication(export);
            Assert.Empty(trace.ToString());
        }
        finally
        {
            PresentationTraceSources.DataBindingSource.Listeners.Remove(listener);
            window.Close();
            app.Shutdown();
        }
    }

    private static void CheckFileDialogsAndImport()
    {
        foreach (var folder in new[] { false, true })
        {
            var dialog = new NameDialog(folder);
            Assert.Equal(WindowStartupLocation.CenterOwner, dialog.WindowStartupLocation);
            Assert.Equal(folder ? "" : ".txt", dialog.Value);
            var content = (FrameworkElement)dialog.Content;
            content.Measure(new Size(600, 400)); content.Arrange(new Rect(0, 0, 600, 400)); content.UpdateLayout();
            dialog.Close();
        }
        var service = new FileUiService();
        var dialogs = new FileUiDialogs();
        var model = new FilesViewModel(service, dialogs);
        model.UpdateSession(new RenderSession().Current);
        service.Owner = model.CurrentUser!.Id;
        var view = new FilesView { DataContext = model };
        view.Measure(new Size(900, 600)); view.Arrange(new Rect(0, 0, 900, 600)); view.UpdateLayout();
        model.LoadAsync("").GetAwaiter().GetResult();
        Assert.Null(model.ErrorMessage);
        model.ImportCommand.Execute(null);
        view.UpdateLayout();
        Assert.Null(model.ErrorMessage);
        Assert.Equal("TEST-import.txt", Assert.Single(model.Items).Name);
        Assert.False(model.IsLoading);
        model.SelectedFile = model.Items[0];
        model.CreateFileCommand.Execute(null);
        model.CreateFolderCommand.Execute(null);
        model.LoadAsync("").GetAwaiter().GetResult();
        view.UpdateLayout();
        Assert.Null(model.ErrorMessage);
        Assert.Equal(3, model.Items.Count);
        model.SearchText = "TEST-new";
        Assert.Single(model.View.Cast<FileListItem>());
        model.SearchText = ""; model.SortIndex = 2;
        Assert.Equal(3, model.View.Cast<FileListItem>().Count());
        using var editorModel = new TextEditorViewModel(service, new RenderSession(), dialogs,
            new(Guid.NewGuid(), "TEST-editor.txt", "TEST content", "UI-only"));
        var editor = new TextEditorWindow(editorModel);
        var editorContent = (FrameworkElement)editor.Content;
        editorContent.Measure(new Size(860, 660)); editorContent.Arrange(new Rect(0, 0, 860, 660)); editorContent.UpdateLayout();
        Assert.True(editor.TryCloseAsync().GetAwaiter().GetResult());
    }

    private static void CheckAuthentication(string? export)
    {
        var stub = new AuthenticationUiStub();
        var model = new AuthenticationWindowViewModel(stub, stub);
        var window = new AuthenticationWindow(model) { ShowActivated = false, Left = -20000, Top = -20000 };
        try
        {
            foreach (var register in new[] { false, true })
            {
                if (register) model.ShowRegisterCommand.Execute(null); else model.ShowLoginCommand.Execute(null);
                foreach (var size in new[] { new Size(640, 760), new Size(520, 600) })
                {
                    window.Width = size.Width; window.Height = size.Height;
                    var root = (FrameworkElement)window.Content;
                    root.Width = size.Width; root.Height = size.Height;
                    root.Measure(size); root.Arrange(new Rect(size));
                    root.UpdateLayout();
                    Assert.Contains(Descendants(root), view => register ? view is RegisterView : view is LoginView);
                    var boxes = Descendants(root).OfType<PasswordBox>().ToList();
                    Assert.Equal(register ? 2 : 1, boxes.Count);
                    Assert.All(boxes, box => { Assert.Empty(box.Password); Assert.Equal(128, box.MaxLength); Assert.True(box.Focusable); });
                    Assert.Contains(Descendants(root).OfType<Button>(), button => button.IsDefault && button.IsEnabled);
                    if (export is not null)
                        foreach (var scale in new[] { 1.0, 1.5 })
                            Export(root, size, scale, Path.Combine(export, $"{(register ? "register" : "login")}-{size.Width}x{size.Height}-{scale * 100:0}pct-render.png"));
                }
            }
        }
        finally { window.Close(); }
    }
    private sealed class FileUiService : IFileService
    {
        public Guid Owner { get; set; }
        public event EventHandler<FileChangeNotification>? Changed;
        private readonly List<FileEntry> _entries = [];
        public Task WaitForIdleAsync() => Task.CompletedTask;
        public Task<FileResult<IReadOnlyList<FileEntry>>> ListAsync(string directory, CancellationToken ct = default)
            => Task.FromResult(new FileResult<IReadOnlyList<FileEntry>>(FileStatus.Success, _entries.ToArray()));
        public Task<FileResult<bool>> ImportAsync(string directory, ImportSelection source, CancellationToken ct = default)
            => Add(source.Name, false);
        private Task<FileResult<bool>> Add(string name, bool folder)
        {
            _entries.Add(new(Guid.NewGuid(), name, name, folder ? Core.Entities.FileKind.Directory : Core.Entities.FileKind.File, 66, DateTime.UtcNow, EntryState.Managed));
            Changed?.Invoke(this, new(Owner));
            return Task.FromResult(new FileResult<bool>(FileStatus.Success, true));
        }
        public Task<FileResult<bool>> CreateAsync(string directory, string name, bool folder, CancellationToken ct = default) => Add(name, folder);
        public Task<FileResult<TextDocument>> ReadAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<FileResult<bool>> SaveAsync(Guid id, string text, string version, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<FileResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<FileResult<bool>> SynchronizeAsync(string relativePath, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<FileResult<bool>> RecoverAsync(CancellationToken ct = default) => throw new NotSupportedException();
    }
    private sealed class FileUiDialogs : IFileDialogs
    {
        public string? AskName(bool folder) => folder ? "TEST-folder" : "TEST-new.txt";
        public ImportSelection? ChooseImport() => new("UI-fixture-only", "TEST-import.txt");
        public bool Confirm(string title, string message) => false;
        public void Information(string message) { }
        public void OpenEditor(TextDocument document) { }
        public Task<bool> CloseEditorsAsync() => Task.FromResult(true);
        public bool? ConfirmUnsaved(string name) => false;
    }
    private sealed class AuthenticationUiStub : IAuthenticationService, IRegistrationService
    {
        public Task<AuthOutcome> LoginAsync(string username, string password, CancellationToken ct = default) => Task.FromResult(AuthOutcome.Invalid());
        public Task<AuthOutcome> LogoutAsync(CancellationToken ct = default) => Task.FromResult(AuthOutcome.Ok());
        public Task<AuthOutcome> RegisterAsync(string username, string password, string confirmation, CancellationToken ct = default) => Task.FromResult(new AuthOutcome(AuthStatus.Validation, "UI test only"));
    }
    private sealed class RenderSession : IUserSession
    {
        public SessionIdentity? Current { get; } = new(Guid.NewGuid(), "ТЕСТ_визуальный_рендер");
        public long Revision => 1;
        public event EventHandler? Changed { add { } remove { } }
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    private static void Export(Visual root, Size size, double scale, string path)
    {
        var bitmap = new RenderTargetBitmap((int)(size.Width * scale), (int)(size.Height * scale),
            96 * scale, 96 * scale, PixelFormats.Pbgra32);
        bitmap.Render(root);
        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        png.Save(stream);
    }
}
