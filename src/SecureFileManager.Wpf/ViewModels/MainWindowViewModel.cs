using System.Windows.Input;
namespace SecureFileManager.Wpf.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    public IReadOnlyList<SectionViewModel> Sections { get; }
    public ICommand NavigateCommand { get; }
    private SectionViewModel _selectedSection;
    public MainWindowViewModel() : this(new DrivesViewModel(), new FilesViewModel(), new ArchivesViewModel(),
        new FormatsViewModel(), new JournalViewModel(), new SettingsViewModel()) { }
    public MainWindowViewModel(DrivesViewModel drives, FilesViewModel files, ArchivesViewModel archives,
        FormatsViewModel formats, JournalViewModel journal, SettingsViewModel settings)
    {
        Sections = Array.AsReadOnly<SectionViewModel>([drives, files, archives, formats, journal, settings]);
        _selectedSection = files;
        NavigateCommand = new RelayCommand(value => SelectedSection = (SectionViewModel)value!,
            value => value is SectionViewModel section && Sections.Contains(section));
    }
    public SectionViewModel SelectedSection
    {
        get => _selectedSection;
        set { if (value is not null && Sections.Contains(value)) SetProperty(ref _selectedSection, value); }
    }
}
