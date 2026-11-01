using System.Collections.ObjectModel;
namespace SecureFileManager.Wpf.ViewModels;

public sealed record FileListItem(string Name, string Type, string Size, string ModifiedAt, string IconKey);
public sealed class FilesViewModel() : SectionViewModel("Мои файлы", "Личное пространство для ваших документов", "Icon.Files")
{
    public override void ResetUserState() { base.ResetUserState(); Items.Clear(); SelectedFile = null; }
    public ObservableCollection<FileListItem> Items { get; } = [];
    public string Breadcrumb => CurrentUser is null ? "Моё хранилище  /  Вход не выполнен" : "Моё хранилище  /  Корень";
    private FileListItem? _selectedFile;
    public FileListItem? SelectedFile
    {
        get => _selectedFile;
        set { if (SetProperty(ref _selectedFile, value)) RaisePropertyChanged(nameof(SelectedCount)); }
    }
    public int SelectedCount => SelectedFile is null ? 0 : 1;
}
