using System.Collections.ObjectModel;
using System.Windows.Input;
using SecureFileManager.Core.Entities;
using SecureFileManager.Core.Journal;
using SecureFileManager.Core.Security;
namespace SecureFileManager.Wpf.ViewModels;

public sealed record AuditListItem(string OccurredAt, string Actor, string Action, string Target, string Result, string Description = "")
{
    public string ResultCode => Result switch { "Успех" => "Success", "Отказано" => "Denied", "Ошибка" => "Failed", _ => "Neutral" };
    public static AuditListItem From(Operation o) => new(o.OccurredAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss"),
        o.ActorNameSnapshot is { Length: <= 50 } name && !name.Any(char.IsControl) ? name : "—",
        o.OperationType.ToString(), RelativePathSyntax.IsValid(o.TargetPathSnapshot) ? o.TargetPathSnapshot! : "—",
        o.Result switch { OperationResult.Success => "Успех", OperationResult.Denied => "Отказано", _ => "Ошибка" }, Describe(o));
    private static string Describe(Operation o) => o.Details switch
    {
        "LOGIN_LOCKED" => "Вход отклонён: временная блокировка.",
        "PERSONAL_STORAGE_PREPARED" => "Регистрация: личное хранилище подготовлено.",
        "METADATA_SYNCHRONIZED" => "Метаданные согласованы по явной команде.",
        "RECOVERY_FINALIZED" => "Завершена подтверждённая операция.",
        "RECOVERY_ROLLED_BACK" => "Отменена незавершённая операция.",
        "STORAGE_DIAGNOSED" => "Неразрушающая диагностика завершена.",
        "STORAGE_DIAGNOSIS_PARTIAL" => "Диагностика неполная; требуется проверка.",
        _ => o.Result == OperationResult.Denied ? "Действие отклонено политикой доступа или безопасности." :
            o.Result == OperationResult.Failed ? "Действие не завершено. Содержимое документа и системные пути скрыты." :
            "Действие выполнено; обязательная запись сохранена."
    };
}
public sealed class JournalViewModel : SectionViewModel
{
    private readonly IJournalService? _journal;
    private long _generation;
    private int _page;
    private bool _hasMore;
    public JournalViewModel(IJournalService? journal = null, AuditStatusViewModel? status = null)
        : base("Журнал", "История собственных действий", "Icon.Journal")
    {
        _journal = journal; Status = status ?? new();
        RefreshCommand = new AsyncRelayCommand(_ => LoadAsync(0), () => Ready);
        NextCommand = new AsyncRelayCommand(_ => LoadAsync(_page + 1), () => Ready && _hasMore && _page < 10000);
        PreviousCommand = new AsyncRelayCommand(_ => LoadAsync(_page - 1), () => Ready && _page > 0);
        ResetCommand = new AsyncRelayCommand(async _ => { ResetFilters(); await LoadAsync(0); }, () => Ready);
    }
    private bool Ready => _journal is not null && CurrentUser is not null && !IsLoading;
    public AuditStatusViewModel Status { get; }
    public ObservableCollection<AuditListItem> Items { get; } = [];
    public IReadOnlyList<string> ActionFilters { get; } = new[] { "Все операции" }.Concat(Enum.GetNames<OperationType>()).ToArray();
    public IReadOnlyList<string> ResultFilters { get; } = ["Все результаты", "Успех", "Отказано", "Ошибка"];
    private int _action, _result;
    private string _search = "";
    private DateTime? _from, _until;
    private bool _descending = true;
    public int ActionIndex { get => _action; set => SetProperty(ref _action, value); }
    public int ResultIndex { get => _result; set => SetProperty(ref _result, value); }
    public string SearchText { get => _search; set => SetProperty(ref _search, value); }
    public DateTime? FromDate { get => _from; set => SetProperty(ref _from, value); }
    public DateTime? UntilDate { get => _until; set => SetProperty(ref _until, value); }
    public bool Descending { get => _descending; set => SetProperty(ref _descending, value); }
    public string PageText => $"Страница {_page + 1} · записей: {Items.Count} · до 50 за запрос";
    public string EmptyText => IsLoading ? "Загрузка…" : ErrorMessage ?? "Нет событий по выбранным условиям.";
    public ICommand RefreshCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand PreviousCommand { get; }
    public ICommand ResetCommand { get; }
    public override Task ActivateAsync() => LoadAsync(0);
    private void ResetFilters() { ActionIndex = 0; ResultIndex = 0; SearchText = ""; FromDate = UntilDate = null; Descending = true; }
    public override void ResetUserState()
    { _generation++; base.ResetUserState(); Items.Clear(); _page = 0; _hasMore = false; ResetFilters(); RaisePropertyChanged(nameof(PageText)); RaisePropertyChanged(nameof(EmptyText)); }
    public async Task LoadAsync(int page = 0)
    {
        if (!Ready) return;
        var generation = ++_generation;
        IsLoading = true; Items.Clear(); ErrorMessage = null;
        CommandManager.InvalidateRequerySuggested(); RaisePropertyChanged(nameof(EmptyText));
        try
        {
            if (ActionIndex < 0 || ActionIndex >= ActionFilters.Count || ResultIndex is < 0 or > 3 ||
                UntilDate?.Date == DateTime.MaxValue.Date) { ErrorMessage = "Проверьте фильтры и диапазон дат."; return; }
            static DateTime? Utc(DateTime? date) => date is null ? null : DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Local).ToUniversalTime();
            var query = new JournalQuery(ActionIndex == 0 ? null : (OperationType)(ActionIndex - 1),
                ResultIndex == 0 ? null : (OperationResult)(ResultIndex - 1), Utc(FromDate), Utc(UntilDate?.AddDays(1)),
                SearchText, Descending, page);
            await _journal!.CheckHealthAsync();
            var result = await _journal.GetAsync(query);
            if (generation != _generation || CurrentUser is null) return;
            if (!result.IsSuccess) { ErrorMessage = result.Status == Core.Results.RepositoryStatus.Validation ?
                "Проверьте диапазон дат и поиск (до 200 символов)." : "Журнал недоступен. Проверьте сеанс и подключение, затем обновите."; return; }
            _page = page; _hasMore = result.Value!.HasMore;
            foreach (var operation in result.Value.Items) Items.Add(AuditListItem.From(operation));
        }
        catch { if (generation == _generation) ErrorMessage = "Не удалось загрузить журнал. Повторите обновление."; }
        finally
        {
            if (generation == _generation) { IsLoading = false; RaisePropertyChanged(nameof(PageText)); RaisePropertyChanged(nameof(EmptyText)); CommandManager.InvalidateRequerySuggested(); }
        }
    }
}
