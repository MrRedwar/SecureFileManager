using System.Collections.ObjectModel;
namespace SecureFileManager.Wpf.ViewModels;

public sealed record FileListItem(string Name, string Type, string Size, string ModifiedAt, string IconKey);
public sealed class FilesViewModel() : SectionViewModel("Мои файлы", "Личное пространство для ваших документов", "Icon.Files")
{
    public ObservableCollection<FileListItem> Items { get; } = [];
    public string Breadcrumb => "Моё хранилище  /  Вход не выполнен";
    private FileListItem? _selectedFile;
    public FileListItem? SelectedFile
    {
        get => _selectedFile;
        set { if (SetProperty(ref _selectedFile, value)) RaisePropertyChanged(nameof(SelectedCount)); }
    }
    public int SelectedCount => SelectedFile is null ? 0 : 1;
}
