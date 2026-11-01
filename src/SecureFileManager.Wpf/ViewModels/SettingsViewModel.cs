namespace SecureFileManager.Wpf.ViewModels;
public sealed class SettingsViewModel() : SectionViewModel("Настройки", "Сведения о приложении и политике безопасности", "Icon.Settings")
{
    public string Version => typeof(SettingsViewModel).Assembly.GetName().Version?.ToString() ?? "Недоступно";
    public string DatabaseStatus => CurrentUser is null ? "Не проверено" : "Подключение и аудит проверены при входе. Непрерывный мониторинг не выполняется.";
    public string StorageStatus => CurrentUser is null ? "Недоступно до входа" : "Личное пространство текущего пользователя";
}
