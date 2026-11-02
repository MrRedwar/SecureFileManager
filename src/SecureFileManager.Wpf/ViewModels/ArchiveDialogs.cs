using SecureFileManager.Core.Files;

namespace SecureFileManager.Wpf.ViewModels;

public sealed record ArchiveTarget(string Directory, string Name);
public interface IArchiveDialogs
{
    ArchiveTarget? ChooseArchiveTarget(bool extract, string directory);
    bool ShowArchivePreview(ArchivePreview preview, bool confirmExtraction);
}
public sealed class ArchiveTargetViewModel : ObservableObject
{
    public FilesViewModel Browser { get; }
    public string Name { get; set; }
    public string Prompt { get; }
    public string InitialDirectory { get; }
    public bool CanAccept => Browser.CurrentUser is not null && !Browser.IsLoading && Browser.ErrorMessage is null;
    public ArchiveTargetViewModel(FilesViewModel browser, string directory, bool extract)
    {
        Browser = browser; InitialDirectory = directory;
        Browser.PropertyChanged += (_, _) => RaisePropertyChanged(nameof(CanAccept));
        Name = extract ? "Извлечено" : "archive.zip";
        Prompt = extract ? "Выберите родительскую папку и имя НОВОЙ папки результата. Существующие папки не объединяются." :
            "Выберите папку назначения в личном хранилище и имя ZIP. Перезапись существующего файла запрещена.";
    }
}
