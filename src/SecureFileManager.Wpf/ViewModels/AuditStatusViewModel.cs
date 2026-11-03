using System.Windows.Threading;
using SecureFileManager.Core.Interfaces;
using SecureFileManager.Core.Journal;
namespace SecureFileManager.Wpf.ViewModels;

public sealed class AuditStatusViewModel : ObservableObject, IDisposable
{
    private readonly IAuditHealth? _health;
    private readonly Dispatcher? _dispatcher;
    private readonly DispatcherTimer? _timer;
    public AuditStatusViewModel(IAuditHealth? health = null, IUserSession? session = null)
    {
        _health = health;
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        _dispatcher = dispatcher?.CheckAccess() == true ? dispatcher : null;
        if (health is not null) health.Changed += OnChanged;
        if (session is not null) session.Changed += (_, _) => { (health as AuditHealth)?.Reset(); };
        if (_dispatcher is not null)
            _timer = new DispatcherTimer(TimeSpan.FromSeconds(15), DispatcherPriority.Background, (_, _) => Refresh(), _dispatcher);
    }
    public string Text => (_health?.Snapshot.State ?? AuditState.Unknown) switch
    {
        AuditState.Active => "Журнал: активен", AuditState.Unavailable => "Журнал: недоступен",
        AuditState.Checking => "Журнал: проверка…", _ => "Журнал: не проверен / проверка устарела"
    };
    public string Evidence => _health?.Snapshot.CheckedUtc is { } time ?
        "Последнее подтверждение: " + time.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss") + ". Статус действителен до 2 минут; доступность может измениться." :
        "Проверка ещё не завершена. Статус не заменяет обязательную запись каждой операции.";
    private void OnChanged(object? sender, EventArgs e)
    {
        if (_dispatcher is not null && !_dispatcher.CheckAccess()) _dispatcher.BeginInvoke(new Action(Refresh)); else Refresh();
    }
    private void Refresh() { RaisePropertyChanged(nameof(Text)); RaisePropertyChanged(nameof(Evidence)); }
    public void Dispose() { if (_health is not null) _health.Changed -= OnChanged; _timer?.Stop(); }
}
