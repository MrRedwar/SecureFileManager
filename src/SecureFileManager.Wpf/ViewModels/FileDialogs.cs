using SecureFileManager.Core.Files;
namespace SecureFileManager.Wpf.ViewModels;
public interface IFileDialogs
{
    string? AskName(bool folder);
    ImportSelection? ChooseImport();
    bool Confirm(string title, string message);
    void Information(string message);
    void OpenEditor(TextDocument document);
    Task<bool> CloseEditorsAsync();
    // true=save, false=discard, null=cancel
    bool? ConfirmUnsaved(string name);
}
