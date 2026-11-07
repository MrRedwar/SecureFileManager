using SecureFileManager.Core.Files;
namespace SecureFileManager.Wpf.ViewModels;

public interface IExternalOperationDialogs
{
    string? AskName(string prompt, string initial);
    string? ChooseDirectory(string initial);
    bool Confirm(ExternalChangeRequest request);
    bool ShowArchivePreview(ArchivePreview preview, bool confirmExtraction) => false;
}
