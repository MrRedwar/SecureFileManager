using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SecureFileManager.Wpf;
using SecureFileManager.Wpf.ViewModels;
using SecureFileManager.Wpf.Views;
using SecureFileManager.Core.Interfaces;
using SecureFileManager.Core.Results;

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
        window.Show();
        var root = (FrameworkElement)window.Content;
        Assert.Equal(3, window.CommandBindings.Count);
        var export = Environment.GetEnvironmentVariable("SFM_UI_ARTIFACT_DIRECTORY");
        if (export is not null) Directory.CreateDirectory(export);
        var expectedTypes = new[] { typeof(DrivesView), typeof(FilesView), typeof(ArchivesView),
            typeof(FormatsView), typeof(JournalView), typeof(SettingsView) };
        try
        {
            foreach (var key in new[] { "Icon.Eye", "Icon.Drives", "Icon.Files", "Icon.Archives", "Icon.JsonXml",
                "Icon.Journal", "Icon.Settings", "Icon.Create", "Icon.Delete", "Icon.Search", "Icon.Security",
                "Icon.User", "Icon.Database", "Art.Anubis", "Art.Header", "Art.Footer", "Texture.Papyrus", "Texture.Stone" })
            {
                var bitmap = Assert.IsAssignableFrom<BitmapSource>(app.FindResource(key));
                Assert.True(bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0, key);
            }
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
                    Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                    root.InvalidateMeasure();
                    root.Measure(size);
                    root.Arrange(new Rect(size));
                    root.UpdateLayout();
                    Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Render);
                    root.UpdateLayout();
                    Assert.Contains(Descendants(root), visual => visual.GetType() == expectedTypes[index]);
                    var navigation = Descendants(root).OfType<ListBox>().Single(x => AutomationProperties.GetName(x) == "Разделы приложения");
                    Assert.Same(section, navigation.SelectedItem);
                    Assert.True(root.ActualWidth <= size.Width && root.ActualHeight <= size.Height);
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
            Assert.Contains(Descendants(root).OfType<ProgressBar>(), bar => bar.IsIndeterminate && bar.IsVisible);
            model.SelectedSection.IsLoading = false;
            model.SelectedSection.ErrorMessage = "Тест состояния: данные недоступны";
            root.UpdateLayout();
            Assert.Contains(Descendants(root).OfType<TextBlock>(), text => text.IsVisible && text.Text == "Не удалось загрузить данные");
            model.SelectedSection.ErrorMessage = null;
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

    private static void CheckAuthentication(string? export)
    {
        var stub = new AuthenticationUiStub();
        var model = new AuthenticationWindowViewModel(stub, stub);
        var window = new AuthenticationWindow(model) { ShowActivated = false, Left = -20000, Top = -20000 };
        try
        {
            window.Show();
            foreach (var register in new[] { false, true })
            {
                if (register) model.ShowRegisterCommand.Execute(null); else model.ShowLoginCommand.Execute(null);
                foreach (var size in new[] { new Size(640, 760), new Size(520, 600) })
                {
                    window.Width = size.Width; window.Height = size.Height;
                    Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                    var root = (FrameworkElement)window.Content;
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
