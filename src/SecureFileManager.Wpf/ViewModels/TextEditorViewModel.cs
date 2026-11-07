using System.Windows.Input;
using SecureFileManager.Core.Files;
using SecureFileManager.Core.Interfaces;
namespace SecureFileManager.Wpf.ViewModels;
public sealed class TextEditorViewModel : ObservableObject, IDisposable
{
    private readonly ITextDocumentService _files; private readonly IUserSession _session; private readonly IFileDialogs _dialogs;
    private readonly long _revision; private readonly Guid _user;
    private readonly Guid _id; private string _version, _original, _text, _error = ""; private bool _busy, _valid = true;
    public string Name { get; }
    public Guid DocumentId => _id;
    public bool IsExternal { get; }
    public string Format => DocumentValidator.Format(Name).ToString().ToUpperInvariant();
    public string FormatLabel => Format + " · UTF-8 · до 10 МиБ";
    public bool IsStructured => Format != "TXT";
    public string DirtyLabel => IsDirty ? "Есть несохранённые изменения" : "Нет несохранённых изменений";
    public string Text { get => _text; set { if (SetProperty(ref _text, value)) { Error = ""; RaisePropertyChanged(nameof(IsDirty)); RaisePropertyChanged(nameof(DirtyLabel)); RaisePropertyChanged(nameof(Title)); CommandManager.InvalidateRequerySuggested(); } } }
    public string Title => Name + (IsDirty ? " *" : "") + " — " + Format;
    public bool IsDirty => _text != _original;
    public bool IsUsable => _valid && !_busy && _session.Revision == _revision && _session.Current?.Id == _user;
    public string Error { get => _error; private set => SetProperty(ref _error, value); }
    public ICommand SaveCommand { get; }
    public ICommand ValidateCommand { get; }
    public ICommand ReloadCommand { get; }
    public event EventHandler? SessionEnded;
    private readonly System.Windows.Threading.Dispatcher? _dispatcher;
    public TextEditorViewModel(ITextDocumentService files, IUserSession session, IFileDialogs dialogs, TextDocument document, bool external = false)
    {
        IsExternal = external;
        _files = files; _session = session; _dialogs = dialogs; _revision = session.Revision; _user = session.Current?.Id ?? Guid.Empty;
        _id = document.Id; Name = document.Name; _version = document.Version; _text = _original = document.Content;
        SaveCommand = new AsyncRelayCommand(_ => SaveAsync(), () => IsUsable && IsDirty);
        ValidateCommand = new AsyncRelayCommand(_ => ValidateAsync(), () => IsUsable && IsStructured);
        ReloadCommand = new AsyncRelayCommand(_ => ReloadAsync(), () => IsUsable);
        Error = document.Validation is { IsValid: false } issue ? issue.Description : "";
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
            if (!result.Succeeded)
            {
                Error = result.Status == FileStatus.Conflict ? "Конфликт: файл изменён или занят. Ваш текст оставлен в редакторе. «Перечитать» загрузит версию с диска после подтверждения; можно продолжить работу без сохранения." : result.Message;
                return false;
            }
            _original = Text; RaisePropertyChanged(nameof(IsDirty)); RaisePropertyChanged(nameof(DirtyLabel)); RaisePropertyChanged(nameof(Title));
            var updated = await _files.ReadAsync(_id);
            if (_session.Revision != _revision) return false;
            if (updated.Succeeded && updated.Value!.Content == _original) { _version = updated.Value.Version; Error = "Сохранено."; }
            else { _version = ""; Error = "Сохранение подтверждено, но версия после записи не подтверждена. Нажмите «Перечитать» перед следующим сохранением."; }
            return true;
        }
        catch { Error = "Сохранение не подтверждено. Исходные данные сохранены для проверки."; return false; }
        finally { _busy = false; RaisePropertyChanged(nameof(IsUsable)); CommandManager.InvalidateRequerySuggested(); }
    }
    public async Task ValidateAsync()
    {
        if (!IsUsable || !IsStructured) return;
        _busy = true; RaisePropertyChanged(nameof(IsUsable)); CommandManager.InvalidateRequerySuggested();
        try
        {
            var result = await _files.ValidateAsync(_id, Text);
            if (_session.Revision == _revision) Error = result.Succeeded ? result.Value!.Description : result.Message;
        }
        catch { if (_session.Revision == _revision) Error = "Проверка не подтверждена. Проверьте доступность базы данных."; }
        finally { _busy = false; RaisePropertyChanged(nameof(IsUsable)); CommandManager.InvalidateRequerySuggested(); }
    }
    public async Task ReloadAsync()
    {
        if (!IsUsable || !_dialogs.Confirm("Перечитать файл?", "Загрузить версию с диска? Несохранённый текст редактора будет потерян только после успешного чтения.")) return;
        _busy = true; RaisePropertyChanged(nameof(IsUsable)); CommandManager.InvalidateRequerySuggested();
        try
        {
            var result = await _files.ReloadAsync(_id);
            if (_session.Revision != _revision) return;
            if (!result.Succeeded) { Error = result.Message; return; }
            _version = result.Value!.Version; _original = result.Value.Content; Text = _original;
            RaisePropertyChanged(nameof(IsDirty)); RaisePropertyChanged(nameof(DirtyLabel)); RaisePropertyChanged(nameof(Title));
            Error = result.Value.Validation is { IsValid: false } issue ? issue.Description : "Версия с диска загружена.";
        }
        catch { if (_session.Revision == _revision) Error = "Не удалось перечитать файл. Текст редактора сохранён."; }
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
