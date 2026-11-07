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
        // The Application constructor queues startup automatically. Abort only work posted
        // by that constructor so layout dispatcher processing cannot launch production login.
        var constructorWork = new List<DispatcherOperation>();
        DispatcherHookEventHandler posted = (_, e) => constructorWork.Add(e.Operation);
        var dispatcher = Dispatcher.CurrentDispatcher; dispatcher.Hooks.OperationPosted += posted;
        App app;
        try { app = new App(); }
        finally { dispatcher.Hooks.OperationPosted -= posted; }
        foreach (var operation in constructorWork) if (operation.Status == DispatcherOperationStatus.Pending) operation.Abort();
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
                Assert.NotNull(graph.GetRequiredService<IComputerBrowseService>());
                Assert.NotNull(graph.GetRequiredService<IExternalDocumentService>());
                Assert.NotNull(graph.GetRequiredService<IExternalFileOperationService>());
                Assert.Same(graph.GetRequiredService<IFileDialogs>(), graph.GetRequiredService<IExternalOperationDialogs>());
                Assert.Same(graph.GetRequiredService<IFileDialogs>(), graph.GetRequiredService<IExternalEditorDialogs>());
                Assert.Same(graph.GetRequiredService<ComputerBrowserViewModel>(), graph.GetRequiredService<DrivesViewModel>().Computer);
                Assert.Same(graph.GetRequiredService<IFileService>(), graph.GetRequiredService<IArchiveService>());
                Assert.Same(graph.GetRequiredService<IFileService>(), graph.GetRequiredService<IStorageDiagnostics>());
                Assert.Same(graph.GetRequiredService<JournalViewModel>().Status, graph.GetRequiredService<SettingsViewModel>().Status);
                Assert.Same(graph.GetRequiredService<IFileDialogs>(), graph.GetRequiredService<IArchiveDialogs>());
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
                        var driveTabs = Descendants(root).OfType<TabControl>().Single(x => AutomationProperties.GetName(x) == "Просмотр дисков и папок");
                        driveTabs.SelectedIndex = 1; root.UpdateLayout();
                        Assert.Contains(Descendants(root), x => x is ComputerBrowserView);
                        var browserGrid = Descendants(root).OfType<DataGrid>().Single(x => AutomationProperties.GetName(x) == "Файлы компьютера");
                        if (export is not null) Export(root, size, 1.5, Path.Combine(export, $"computer-{size.Width}x{size.Height}-150pct-render.png"));
                        Assert.True(browserGrid.ActualHeight >= 70, $"Computer browser table {browserGrid.ActualWidth}x{browserGrid.ActualHeight}, root {size}.");
                        Assert.Contains(Descendants(root).OfType<TextBox>(), x => AutomationProperties.GetName(x) == "Путь к папке компьютера");
                        var operationsMenu = Descendants(root).OfType<Menu>().Single(x => AutomationProperties.GetName(x) == "Действия с файлами компьютера");
                        var commands = ((MenuItem)operationsMenu.Items[0]).Items.OfType<MenuItem>().ToArray();
                        Assert.Equal(11, commands.Length);
                        Assert.All(commands, x => Assert.NotNull(x.Command));
                        // Sorting uses raw bytes, not formatted strings such as "10 КБ" and "2 КБ".
                        drivesModel.Computer.Items.Add(new(new("ui-large", "large", false, 10240, null, false)));
                        drivesModel.Computer.Items.Add(new(new("ui-small", "small", false, 2048, null, false)));
                        Assert.Equal("Data.SizeBytes", browserGrid.Columns[2].SortMemberPath);
                        browserGrid.Items.SortDescriptions.Add(new("Data.SizeBytes", System.ComponentModel.ListSortDirection.Ascending));
                        Assert.Equal("small", Assert.IsType<ComputerListItem>(browserGrid.Items[0]).Name);
                        browserGrid.Items.SortDescriptions.Clear(); drivesModel.Computer.Items.Clear();
                        driveTabs.SelectedIndex = 0; root.UpdateLayout();
                    }
                    var navigation = Descendants(root).OfType<ListBox>().Single(x => AutomationProperties.GetName(x) == "Разделы приложения");
                    Assert.Same(section, navigation.SelectedItem);
                    Assert.True(root.ActualWidth <= size.Width && root.ActualHeight <= size.Height,
                        $"Actual layout {root.ActualWidth:R}x{root.ActualHeight:R}; target {size.Width}x{size.Height}");
                    foreach (var grid in Descendants(root).OfType<DataGrid>())
                    {
                        Assert.Empty(grid.Items);
                        Assert.True(grid.ActualWidth > 0);
                        var chain = new List<string>();
                        for (DependencyObject? parent = grid; parent is not null; parent = VisualTreeHelper.GetParent(parent))
                            if (parent is FrameworkElement element) chain.Add($"{element.GetType().Name}={element.ActualWidth}x{element.ActualHeight} desired={element.DesiredSize}" + (element is Grid g ? " rows=" + string.Join(",", g.RowDefinitions.Select(r => r.ActualHeight)) : ""));
                        Assert.True(grid.ActualHeight >= 70, $"{section.Title} table: " + string.Join(" > ", chain));
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
            CheckStructuredBrowserAndEditor(export);
            CheckArchiveUi(export);
            CheckJournalUi(export);
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

    private sealed class JournalUiService : Core.Journal.IJournalService
    {
        public Core.Journal.JournalQuery? Last { get; private set; }
        public Task CheckHealthAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task<Core.Results.RepositoryResult<Core.Journal.JournalPage>> GetAsync(Core.Journal.JournalQuery query, CancellationToken ct = default)
        {
            Last = query;
            return Task.FromResult(Core.Results.RepositoryResult<Core.Journal.JournalPage>.Ok(new([
                new Core.Entities.Operation { UserId = Guid.NewGuid(), OperationType = Core.Entities.OperationType.FILE_CREATED,
                    Result = Core.Entities.OperationResult.Success, TargetPathSnapshot = "TEST-journal.txt", Details = "FILE_TX_Create" },
                new Core.Entities.Operation { OperationType = Core.Entities.OperationType.ACCESS_DENIED, Result = Core.Entities.OperationResult.Denied }
            ], query.Page == 0)));
        }
    }
    private static void CheckJournalUi(string? export)
    {
        var service = new JournalUiService(); var clock = new Core.Services.SystemClock(); var health = new Core.Journal.AuditHealth(clock);
        using var status = new AuditStatusViewModel(health);
        var model = new JournalViewModel(service, status); model.UpdateSession(new RenderSession().Current);
        model.LoadAsync().GetAwaiter().GetResult(); Assert.Equal(2, model.Items.Count);
        var view = new JournalView { DataContext = model }; var size = new Size(690, 300);
        var canvas = new Border { Background = (Brush)Application.Current.FindResource("Brush.Sand100"), Child = view };
        canvas.Measure(size); canvas.Arrange(new Rect(size)); canvas.UpdateLayout();
        var grid = Descendants(view).OfType<DataGrid>().Single(); Assert.True(grid.IsReadOnly); Assert.False(grid.CanUserSortColumns);
        Assert.Equal(2, grid.Items.Count); Assert.True(model.NextCommand.CanExecute(null));
        model.NextCommand.Execute(null); Assert.Equal(1, service.Last!.Page); Assert.False(model.NextCommand.CanExecute(null));
        model.PreviousCommand.Execute(null); Assert.Equal(0, service.Last.Page);
        model.SearchText = "TEST"; model.FromDate = new DateTime(2026, 11, 1); model.UntilDate = new DateTime(2026, 11, 2);
        model.ResultIndex = 2; model.Descending = false; model.RefreshCommand.Execute(null);
        Assert.Equal(Core.Entities.OperationResult.Denied, service.Last.Result); Assert.False(service.Last.Descending);
        Assert.Equal(DateTimeKind.Utc, service.Last.FromUtc!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, service.Last.UntilUtc!.Value.Kind);
        Assert.Equal("TEST", service.Last.Search);
        // Scroll to the final column: horizontal scrolling cannot reveal text clipped inside a fixed-width cell.
        canvas.Measure(size); canvas.Arrange(new Rect(size)); canvas.UpdateLayout();
        grid.ScrollIntoView(model.Items[1], grid.Columns[^1]);
        canvas.Dispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle); canvas.UpdateLayout();
        var description = Descendants(grid).OfType<TextBlock>().First(x => ReferenceEquals(x.DataContext, model.Items[1]) && x.Text == model.Items[1].Description);
        Assert.True(description.ActualHeight > 30, "Long audit descriptions must be readable on multiple lines.");
        Assert.Equal(model.Items[1].Description, description.ToolTip);
        var row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromItem(model.Items[1]);
        Assert.True(row.ActualHeight >= description.ActualHeight + 8, "The complete wrapped description must fit in the row.");
        foreach (var expanded in new[] { false, true })
        {
            Descendants(view).OfType<Expander>().Single().IsExpanded = expanded;
            canvas.Measure(size); canvas.Arrange(new Rect(size)); canvas.UpdateLayout();
            Assert.True(grid.ActualHeight >= 85, "Journal filters must leave a scrollable table.");
            if (export is not null) Export(canvas, size, 1.5, Path.Combine(export, $"journal-populated-filters-{expanded}-150pct-render.png"));
        }
        health.Failed(); Assert.Contains("недоступен", model.Status.Text);
        model.UpdateSession(null); Assert.Empty(model.Items); Assert.Empty(model.SearchText);
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
    private static void CheckStructuredBrowserAndEditor(string? export)
    {
        var session = new RenderSession(); var dialogs = new FileUiDialogs(); var service = new FileUiService();
        var formats = new FormatsViewModel(service, dialogs); formats.UpdateSession(session.Current);
        foreach (var name in new[] { "test.txt", "test.json", "test.xml" })
            service.CreateAsync("", name, false).GetAwaiter().GetResult();
        service.CreateAsync("", "folder", true).GetAwaiter().GetResult();
        formats.ActivateAsync().GetAwaiter().GetResult();
        Assert.Equal(new[] { "folder", "test.json", "test.xml" }, formats.Browser.View.Cast<FileListItem>().Select(x => x.Name));
        Assert.Same(session.Current, formats.Browser.CurrentUser);
        formats.UpdateSession(null); Assert.Empty(formats.Browser.Items); Assert.Null(formats.Browser.CurrentUser);
        foreach (var name in new[] { "TEST-editor.txt", "TEST-editor.json", "TEST-editor.xml" })
        {
            var format = DocumentValidator.Format(name);
            var text = format == DocumentFormat.Json ? "{\n  \"имя\": \"ТЕСТ\"\n}" : format == DocumentFormat.Xml ? "<root>\n  ТЕСТ\n</root>" : "ТЕСТ\nстрока";
            using var model = new TextEditorViewModel(service, session, dialogs, new(Guid.NewGuid(), name, text, "UI-only"));
            var window = new TextEditorWindow(model);
            var root = (FrameworkElement)window.Content;
            foreach (var size in new[] { new Size(860, 660), new Size(600, 440) })
            {
                root.Width = size.Width; root.Height = size.Height;
                root.Measure(size); root.Arrange(new Rect(size)); root.UpdateLayout();
                var input = Descendants(root).OfType<TextBox>().Single();
                Assert.Equal(text, input.Text); Assert.False(model.IsDirty); Assert.True(input.IsEnabled);
                Assert.Equal(format != DocumentFormat.Txt, model.ValidateCommand.CanExecute(null));
                input.Text += " "; input.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
                Assert.True(model.IsDirty); Assert.Contains("*", model.Title);
                Assert.True(model.SaveCommand.CanExecute(null));
                if (export is not null) Export(root, size, 1.5, Path.Combine(export, $"editor-{format}-{size.Width}x{size.Height}-150pct-render.png"));
                model.Text = text; root.UpdateLayout();
            }
            Assert.True(window.TryCloseAsync().GetAwaiter().GetResult());
        }
    }
    private sealed class FileUiService : IFileService, IArchiveService
    {
        public IReadOnlyList<Guid> ArchivedIds { get; private set; } = [];
        public Task<FileResult<bool>> CreateArchiveAsync(IReadOnlyList<Guid> sources, string directory, string name, IProgress<ArchiveProgress>? progress = null, CancellationToken ct = default)
        { ArchivedIds = sources.ToArray(); return Add(name, false); }
        public Task<FileResult<ArchivePreview>> PreviewArchiveAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(new FileResult<ArchivePreview>(FileStatus.Success, new([new("TEST.txt", false, 4, 4)], "UI-only")));
        public Task<FileResult<bool>> ExtractArchiveAsync(Guid id, string directory, string folderName, string expectedVersion, IProgress<ArchiveProgress>? progress = null, CancellationToken ct = default)
            => Add(folderName, true);
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
        public Task<FileResult<TextDocument>> ReloadAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<FileResult<DocumentValidation>> ValidateAsync(Guid id, string text, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<FileResult<bool>> SaveAsync(Guid id, string text, string version, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<FileResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<FileResult<bool>> SynchronizeAsync(string relativePath, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<FileResult<bool>> RecoverAsync(CancellationToken ct = default) => throw new NotSupportedException();
    }
    private sealed class ArchiveUiDialogs : IFileDialogs, IArchiveDialogs
    {
        public int Previews { get; private set; }
        public ArchiveTarget? ChooseArchiveTarget(bool extract, string directory) => new(directory, extract ? "TEST-extracted" : "TEST-created.zip");
        public bool ShowArchivePreview(ArchivePreview preview, bool confirmExtraction) { Previews++; return preview.CanExtract; }
        public bool Confirm(string title, string message) => true;
        public string? AskName(bool folder) => null;
        public ImportSelection? ChooseImport() => null;
        public void Information(string message) { }
        public void OpenEditor(TextDocument document) { }
        public Task<bool> CloseEditorsAsync() => Task.FromResult(true);
        public bool? ConfirmUnsaved(string name) => null;
    }
    private static void CheckArchiveUi(string? export)
    {
        var session = new RenderSession(); var service = new FileUiService(); var dialogs = new ArchiveUiDialogs();
        using var model = new FilesViewModel(service, dialogs); model.UpdateSession(session.Current); service.Owner = session.Current!.Id;
        service.CreateAsync("", "TEST-a.txt", false).GetAwaiter().GetResult();
        service.CreateAsync("", "TEST-folder", true).GetAwaiter().GetResult();
        model.LoadAsync("").GetAwaiter().GetResult();
        var view = new FilesView { DataContext = model };
        view.Measure(new Size(900, 600)); view.Arrange(new Rect(0, 0, 900, 600)); view.UpdateLayout();
        var grid = Descendants(view).OfType<DataGrid>().Single(); Assert.Equal(DataGridSelectionMode.Extended, grid.SelectionMode);
        grid.SelectedItems.Add(model.Items[0]); grid.SelectedItems.Add(model.Items[1]); view.UpdateLayout();
        Assert.Equal(2, model.SelectedCount); Assert.True(model.ArchiveCommand.CanExecute(null));
        Assert.False(model.OpenCommand.CanExecute(null)); Assert.False(model.DeleteCommand.CanExecute(null));
        model.ArchiveCommand.Execute(null); view.UpdateLayout(); Assert.Equal(2, service.ArchivedIds.Count);
        Assert.Contains(model.Items, x => x.Name == "TEST-created.zip"); Assert.Null(model.ErrorMessage);
        grid.SelectedItem = model.Items.Single(x => x.Name.EndsWith(".zip")); view.UpdateLayout();
        Assert.True(model.PreviewArchiveCommand.CanExecute(null)); model.PreviewArchiveCommand.Execute(null);
        Assert.Equal(1, dialogs.Previews);
        model.ExtractArchiveCommand.Execute(null); view.UpdateLayout();
        Assert.Equal(2, dialogs.Previews); Assert.Contains(model.Items, x => x.Name == "TEST-extracted"); Assert.Null(model.ErrorMessage);
        var archiveSection = new ArchivesViewModel(service, dialogs); archiveSection.UpdateSession(session.Current);
        archiveSection.ActivateAsync().GetAwaiter().GetResult();
        Assert.All(archiveSection.Browser.View.Cast<FileListItem>(), x => Assert.True(x.Entry!.Kind == Core.Entities.FileKind.Directory || x.Name.EndsWith(".zip")));
        archiveSection.UpdateSession(null); Assert.Empty(archiveSection.Browser.Items); Assert.Empty(archiveSection.Sources.Items);
        archiveSection.Browser.Dispose(); archiveSection.Sources.Dispose();
        var safe = new ArchivePreview([new("folder/TEST.txt", false, 4, 4)], "UI-only");
        var unsafePreview = new ArchivePreview([new("../TEST", false, 1, 1, "Опасный путь")], "UI-only");
        foreach (var preview in new[] { safe, unsafePreview })
        {
            var window = new ArchivePreviewWindow(preview, true); var original = (FrameworkElement)window.Content;
            window.Content = null; var content = new Border { Background = window.Background, Child = original }; window.Content = content;
            foreach (var size in new[] { new Size(900, 620), new Size(600, 440) })
            {
                content.Width = size.Width; content.Height = size.Height; content.Measure(size); content.Arrange(new Rect(size)); content.UpdateLayout();
                Assert.Equal(preview.CanExtract, Descendants(content).OfType<Button>().Single(x => x.Content?.ToString()?.StartsWith("Далее") == true).IsEnabled);
                if (export is not null) Export(content, size, 1.5, Path.Combine(export, $"zip-preview-{preview.CanExtract}-{size.Width}x{size.Height}-150pct-render.png"));
            }
            window.Close();
        }
        using var folderBrowser = new FilesViewModel(service, directoriesOnly: true); folderBrowser.UpdateSession(session.Current);
        folderBrowser.LoadAsync("").GetAwaiter().GetResult(); Assert.All(folderBrowser.View.Cast<FileListItem>(), x => Assert.Equal(Core.Entities.FileKind.Directory, x.Entry!.Kind));
        var target = new ArchiveTargetWindow(new(folderBrowser, "", false)); var targetRoot = (FrameworkElement)target.Content;
        target.Content = null; var targetContent = new Border { Background = target.Background, Child = targetRoot }; target.Content = targetContent;
        targetContent.Measure(new Size(600, 440)); targetContent.Arrange(new Rect(0, 0, 600, 440)); targetContent.UpdateLayout();
        targetContent.Dispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle); targetContent.UpdateLayout();
        Assert.Contains(Descendants(targetContent).OfType<TextBlock>(), x => x.Text == "TEST-folder");
        Assert.True(Descendants(targetContent).OfType<DataGrid>().Single().Columns[0].ActualWidth > 0);
        if (export is not null) Export(targetContent, new Size(600, 440), 1.5, Path.Combine(export, "zip-target-600x440-150pct-render.png"));
        target.Close();
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
