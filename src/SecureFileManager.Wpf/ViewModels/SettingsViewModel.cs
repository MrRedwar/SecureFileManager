namespace SecureFileManager.Wpf.ViewModels;
public sealed class SettingsViewModel() : SectionViewModel("Настройки", "Сведения о приложении и политике безопасности", "Icon.Settings")
{
    public string Version => typeof(SettingsViewModel).Assembly.GetName().Version?.ToString() ?? "Недоступно";
    public string DatabaseStatus => "Не проверено в текущем запуске";
    public string StorageStatus => "Недоступно до входа";
}
