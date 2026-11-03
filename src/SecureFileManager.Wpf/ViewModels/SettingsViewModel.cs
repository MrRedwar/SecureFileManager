using System.Collections.ObjectModel;
using System.Windows.Input;
using SecureFileManager.Core.Files;
using SecureFileManager.Core.Journal;
namespace SecureFileManager.Wpf.ViewModels;
public sealed record StorageIssueItem(string Code, string Path, string Description);
public sealed class SettingsViewModel : SectionViewModel
{
    private readonly IJournalService? _journal;
    private readonly IStorageDiagnostics? _diagnostics;
    private long _generation;
    public SettingsViewModel(IJournalService? journal = null, IStorageDiagnostics? diagnostics = null, AuditStatusViewModel? status = null)
        : base("Настройки", "Сведения о приложении и политике безопасности", "Icon.Settings")
    {
        _journal = journal; _diagnostics = diagnostics; Status = status ?? new();
        CheckCommand = new AsyncRelayCommand(_ => CheckAsync(), () => CurrentUser is not null && !IsLoading && _journal is not null);
        DiagnoseCommand = new AsyncRelayCommand(_ => DiagnoseAsync(), () => CurrentUser is not null && !IsLoading && _diagnostics is not null);
    }
    public AuditStatusViewModel Status { get; }
    public string Version => typeof(SettingsViewModel).Assembly.GetName().Version?.ToString() ?? "Недоступно";
    public string DatabaseStatus => Status.Text;
    public string StorageStatus => CurrentUser is null ? "Недоступно до входа" : "Личное пространство текущего пользователя";
    public ICommand CheckCommand { get; }
    public ICommand DiagnoseCommand { get; }
    public ObservableCollection<StorageIssueItem> Issues { get; } = [];
    private string _diagnosisStatus = "Диагностика ещё не выполнялась.";
    public string DiagnosisStatus { get => _diagnosisStatus; private set => SetProperty(ref _diagnosisStatus, value); }
    public override Task ActivateAsync() => CheckAsync();
    public override void ResetUserState() { _generation++; base.ResetUserState(); Issues.Clear(); DiagnosisStatus = "Диагностика ещё не выполнялась."; }
    public async Task CheckAsync()
    {
        if (_journal is null || CurrentUser is null || IsLoading) return;
        var generation = _generation; IsLoading = true;
        try { await _journal.CheckHealthAsync(); }
        catch { if (generation == _generation) ErrorMessage = "Не удалось проверить аудит. Повторите проверку."; }
        finally { if (generation == _generation) { IsLoading = false; CommandManager.InvalidateRequerySuggested(); } }
    }
    public async Task DiagnoseAsync()
    {
        if (_diagnostics is null || CurrentUser is null || IsLoading) return;
        var generation = ++_generation; IsLoading = true; Issues.Clear(); ErrorMessage = null; DiagnosisStatus = "Проверка хранилища…";
        try
        {
            var result = await _diagnostics.DiagnoseAsync();
            if (generation != _generation) return;
            if (!result.Succeeded) { ErrorMessage = result.Message; DiagnosisStatus = "Диагностика не завершена."; return; }
            foreach (var issue in result.Value!.Issues)
                Issues.Add(new(issue.Code, issue.Path ?? "—", issue.Code switch
                {
                    "UNTRACKED" => "Объект вне БД; автоматически не присваивается.",
                    "METADATA_NOT_CHECKED" => "Объект не сопоставлен: достигнут предел выборки метаданных.",
                    "MISSING" => "Объект отсутствует на диске; запись сохранена.",
                    "METADATA_CHANGED" => "Тип, размер или дата не совпадают.",
                    "DELETED_PRESENT" => "Soft-delete, но объект остался на диске.",
                    "PENDING_OPERATION" => "Незавершённая операция; доступно прежнее восстановление.",
                    "PENDING_COMMITTED" => "SQL подтверждает успех; требуется завершение очистки.",
                    "PENDING_UNCOMMITTED" => "SQL не подтверждает успех; требуется проверяемая компенсация.",
                    "INCOMPLETE_STAGING" => "Неизвестный/повреждённый staging; нужен ручной разбор.",
                    "AUDIT_METADATA_MISMATCH" => "История и метаданные требуют сверки.",
                    _ => "Объект недоступен или небезопасен; не обходится."
                }));
            DiagnosisStatus = $"Проверено объектов: {result.Value.CheckedObjects}; замечаний: {Issues.Count}. " +
                (result.Value.Complete ? "Обход завершён." : "Проверка НЕПОЛНАЯ: достигнут лимит или часть каталогов недоступна.") + " Данные не исправлялись.";
        }
        catch { if (generation == _generation) { ErrorMessage = "Диагностика недоступна. Данные не исправлялись."; DiagnosisStatus = "Диагностика не завершена."; } }
        finally { if (generation == _generation) { IsLoading = false; CommandManager.InvalidateRequerySuggested(); } }
    }
}
