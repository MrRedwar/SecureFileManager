using System.Windows;
using Microsoft.Win32;
using SecureFileManager.Core.Files;
using SecureFileManager.Core.Interfaces;
using SecureFileManager.Wpf.ViewModels;
using SecureFileManager.Wpf.Views;
namespace SecureFileManager.Wpf.Infrastructure.Dialogs;
public sealed class WindowsFileDialogs(IFileService files, IUserSession session) : IFileDialogs, IArchiveDialogs
{
    private readonly HashSet<TextEditorWindow> _editors = [];
    public ArchiveTarget? ChooseArchiveTarget(bool extract, string directory)
    {
        var browser = new FilesViewModel(files, directoriesOnly: true); browser.UpdateSession(session.Current);
        var model = new ArchiveTargetViewModel(browser, directory, extract);
        var window = new ArchiveTargetWindow(model) { Owner = Application.Current.MainWindow };
        EventHandler ended = (_, _) => window.Dispatcher.Invoke(() => window.Close());
        session.Changed += ended;
        try { return window.ShowDialog() == true ? new(browser.CurrentDirectory, model.Name) : null; }
        finally { session.Changed -= ended; browser.Dispose(); }
    }
    public bool ShowArchivePreview(ArchivePreview preview, bool confirmExtraction)
    {
        var window = new ArchivePreviewWindow(preview, confirmExtraction) { Owner = Application.Current.MainWindow };
        EventHandler ended = (_, _) => window.Dispatcher.Invoke(() => window.Close());
        session.Changed += ended;
        try { return window.ShowDialog() == true; }
        finally { session.Changed -= ended; }
    }
    public string? AskName(bool folder)
    {
        var dialog = new NameDialog(folder) { Owner = Application.Current.MainWindow };
        return dialog.ShowDialog() == true ? dialog.Value : null;
    }
    public ImportSelection? ChooseImport()
    {
        var picker = new OpenFileDialog { Title = "Импортировать файл (копия в личное хранилище)",
            CheckFileExists = true, Multiselect = false, DereferenceLinks = false, Filter = "Все файлы|*.*" };
        return picker.ShowDialog(Application.Current.MainWindow) == true ? new(picker.FileName, picker.SafeFileName) : null;
    }
    public bool Confirm(string title, string message) => MessageBox.Show(Application.Current.MainWindow, message, title,
        MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
    public void Information(string message) => MessageBox.Show(Application.Current.MainWindow, message, "Сведения", MessageBoxButton.OK, MessageBoxImage.Information);
    public bool? ConfirmUnsaved(string name) => MessageBox.Show(Application.Current.MainWindow, $"Сохранить изменения «{name}»?",
        "Несохранённые изменения", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) switch
        { MessageBoxResult.Yes => true, MessageBoxResult.No => false, _ => null };
    public void OpenEditor(TextDocument document)
    {
        var existing = _editors.FirstOrDefault(window => ((TextEditorViewModel)window.DataContext).DocumentId == document.Id);
        if (existing is not null) { existing.Activate(); return; }
        var model = new TextEditorViewModel(files, session, this, document);
        var window = new TextEditorWindow(model) { Owner = Application.Current.MainWindow };
        _editors.Add(window); window.Closed += (_, _) => { _editors.Remove(window); model.Dispose(); };
        window.Show();
    }
    public async Task<bool> CloseEditorsAsync()
    {
        foreach (var editor in _editors.ToArray()) if (!await editor.TryCloseAsync()) return false;
        return true;
    }
}
