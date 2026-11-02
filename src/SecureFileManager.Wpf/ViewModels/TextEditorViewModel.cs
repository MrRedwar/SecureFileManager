using System.Windows.Input;
using SecureFileManager.Core.Files;
using SecureFileManager.Core.Interfaces;
namespace SecureFileManager.Wpf.ViewModels;
public sealed class TextEditorViewModel : ObservableObject, IDisposable
{
    private readonly IFileService _files; private readonly IUserSession _session; private readonly IFileDialogs _dialogs;
    private readonly long _revision; private readonly Guid _user;
    private readonly Guid _id; private string _version, _original, _text, _error = ""; private bool _busy, _valid = true;
    public string Name { get; }
    public string Text { get => _text; set { if (SetProperty(ref _text, value)) { RaisePropertyChanged(nameof(IsDirty)); RaisePropertyChanged(nameof(Title)); CommandManager.InvalidateRequerySuggested(); } } }
    public string Title => Name + (IsDirty ? " *" : "") + " — TXT";
    public bool IsDirty => _text != _original;
    public bool IsUsable => _valid && !_busy && _session.Revision == _revision && _session.Current?.Id == _user;
    public string Error { get => _error; private set => SetProperty(ref _error, value); }
    public ICommand SaveCommand { get; }
    public event EventHandler? SessionEnded;
    private readonly System.Windows.Threading.Dispatcher? _dispatcher;
    public TextEditorViewModel(IFileService files, IUserSession session, IFileDialogs dialogs, TextDocument document)
    {
        _files = files; _session = session; _dialogs = dialogs; _revision = session.Revision; _user = session.Current?.Id ?? Guid.Empty;
        _id = document.Id; Name = document.Name; _version = document.Version; _text = _original = document.Content;
        SaveCommand = new AsyncRelayCommand(_ => SaveAsync(), () => IsUsable && IsDirty);
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        _dispatcher = dispatcher?.CheckAccess() == true ? dispatcher : null;
        session.Changed += OnSessionChanged;
    }
    private void OnSessionChanged(object? sender, EventArgs e)
    {
        void Clear()
        {
            _valid = false; _original = ""; Text = ""; RaisePropertyChanged(nameof(IsUsable));
            SessionEnded?.Invoke(this, EventArgs.Empty);
        }
        if (_dispatcher is not null && !_dispatcher.CheckAccess()) _dispatcher.Invoke(Clear); else Clear();
    }
    public async Task<bool> SaveAsync()
    {
        if (!IsUsable) return false;
        _busy = true; Error = ""; RaisePropertyChanged(nameof(IsUsable)); CommandManager.InvalidateRequerySuggested();
        try
        {
            var result = await _files.SaveAsync(_id, Text, _version);
            if (_session.Revision != _revision) return false;
            if (!result.Succeeded) { Error = result.Message; return false; }
            _original = Text; RaisePropertyChanged(nameof(IsDirty)); RaisePropertyChanged(nameof(Title));
            var updated = await _files.ReadAsync(_id);
            if (_session.Revision != _revision) return false;
            if (updated.Succeeded) _version = updated.Value!.Version;
            else { _valid = false; Error = "Сохранение подтверждено, но повторное чтение не удалось. Закройте и откройте файл заново."; }
            return true;
        }
        catch { Error = "Сохранение не подтверждено. Исходные данные сохранены для проверки."; return false; }
        finally { _busy = false; RaisePropertyChanged(nameof(IsUsable)); CommandManager.InvalidateRequerySuggested(); }
    }
    public async Task<bool> CanCloseAsync()
    {
        if (_busy) return false;
        if (!IsDirty || !_valid || _session.Revision != _revision) return true;
        var answer = _dialogs.ConfirmUnsaved(Name);
        return answer is false || (answer is true && await SaveAsync());
    }
    public void Dispose()
    {
        _session.Changed -= OnSessionChanged; _valid = false; _text = _original = ""; _version = "";
    }
}
