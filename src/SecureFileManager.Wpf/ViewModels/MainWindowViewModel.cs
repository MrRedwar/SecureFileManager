namespace SecureFileManager.Wpf.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    public IReadOnlyList<SectionViewModel> Sections { get; } = Array.AsReadOnly(new[]
    {
        new SectionViewModel("Диски", "Просмотр характеристик накопителей будет добавлен на этапе 05."),
        new SectionViewModel("Мои файлы", "Личное хранилище будет доступно после реализации входа и проверки прав на этапах 04–05."),
        new SectionViewModel("Архивы", "Создание и безопасная распаковка ZIP будут добавлены на этапе 07."),
        new SectionViewModel("JSON / XML", "Редактор и проверка форматов будут добавлены на этапе 06."),
        new SectionViewModel("Журнал", "Просмотр собственных событий будет добавлен на этапе 08. Аудит пока не подключён."),
        new SectionViewModel("Настройки", "Настройки пользователя и состояние базы данных будут добавлены на следующих этапах.")
    });

    private SectionViewModel _selectedSection;

    public MainWindowViewModel() => _selectedSection = Sections[1];

    public SectionViewModel SelectedSection
    {
        get => _selectedSection;
        set
        {
            if (value is not null && Sections.Contains(value))
                SetProperty(ref _selectedSection, value);
        }
    }
}
