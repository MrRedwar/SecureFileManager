using SecureFileManager.Core.Interfaces;
using SecureFileManager.Core.Services;
using System.Windows.Input;
namespace SecureFileManager.Wpf.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    public IReadOnlyList<SectionViewModel> Sections { get; }
    private readonly IUserSession _session;
    public string SessionStatus => _session.Current?.Username ?? "Не авторизован";
    public bool IsAuthenticated => _session.Current is not null;
    public string StorageStatus => IsAuthenticated ? "Личное хранилище" : "Недоступно до входа";
    public event EventHandler? LogoutRequested;
    public ICommand LogoutCommand { get; }
    public ICommand NavigateCommand { get; }
    private SectionViewModel _selectedSection;
    public MainWindowViewModel() : this(new DrivesViewModel(), new FilesViewModel(), new ArchivesViewModel(),
        new FormatsViewModel(), new JournalViewModel(), new SettingsViewModel()) { }
    public MainWindowViewModel(DrivesViewModel drives, FilesViewModel files, ArchivesViewModel archives,
        FormatsViewModel formats, JournalViewModel journal, SettingsViewModel settings)
        : this(drives, files, archives, formats, journal, settings, new UserSession()) { }
    public MainWindowViewModel(DrivesViewModel drives, FilesViewModel files, ArchivesViewModel archives,
        FormatsViewModel formats, JournalViewModel journal, SettingsViewModel settings, IUserSession session)
    {
        _session = session;
        LogoutCommand = new RelayCommand(_ => LogoutRequested?.Invoke(this, EventArgs.Empty), _ => IsAuthenticated);
        Sections = Array.AsReadOnly<SectionViewModel>([drives, files, archives, formats, journal, settings]);
        _selectedSection = files;
        foreach (var section in Sections) section.UpdateSession(session.Current);
        // Capture only the dispatcher owning this VM, not a stale Application from another thread.
        var ownerDispatcher = System.Windows.Application.Current?.Dispatcher;
        if (ownerDispatcher is not null && !ownerDispatcher.CheckAccess()) ownerDispatcher = null;
        session.Changed += (_, _) =>
        {
            void Update()
            {
                foreach (var section in Sections) section.UpdateSession(session.Current);
                if (session.Current is null) SelectedSection = files;
                RaisePropertyChanged(nameof(SessionStatus)); RaisePropertyChanged(nameof(IsAuthenticated));
                RaisePropertyChanged(nameof(StorageStatus));
                CommandManager.InvalidateRequerySuggested();
            }
            if (ownerDispatcher is not null && !ownerDispatcher.HasShutdownStarted &&
                !ownerDispatcher.HasShutdownFinished && !ownerDispatcher.CheckAccess()) ownerDispatcher.Invoke(Update);
            else Update();
        };
        NavigateCommand = new RelayCommand(value => SelectedSection = (SectionViewModel)value!,
            value => value is SectionViewModel section && Sections.Contains(section));
    }
    public SectionViewModel SelectedSection
    {
        get => _selectedSection;
        set { if (value is not null && Sections.Contains(value) && SetProperty(ref _selectedSection, value)) _ = value.ActivateAsync(); }
    }
    public Task ActivateCurrentAsync() => SelectedSection.ActivateAsync();
}
