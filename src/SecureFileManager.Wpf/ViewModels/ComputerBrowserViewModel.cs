using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using SecureFileManager.Core.Files;

namespace SecureFileManager.Wpf.ViewModels;

public sealed record ComputerListItem(ComputerEntry Data)
{
    public string Name => Data.Name;
    public string Kind => Data.IsDirectory ? "Папка / диск" : "Файл";
    public string Size => Data.SizeBytes is { } size ? FileDisplay.Bytes(size) : "—";
    public string Modified => Data.ModifiedUtc?.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss") ?? "—";
    public string Note => Data.Note;
}

public sealed class ComputerBrowserViewModel : SectionViewModel
{
    private readonly IComputerBrowseService? _browser;
    private readonly IExternalDocumentService? _documents;
    private readonly IExternalEditorDialogs? _dialogs;
    private readonly IExternalFileOperationService? _operations;
    private readonly IExternalOperationDialogs? _operationDialogs;
    private long _generation;
    private string _path = "", _address = "", _search = "", _status = "Выберите диск или укажите путь к локальной папке.";
    private string? _parent;
    private CancellationTokenSource? _pending;
    private ComputerListItem? _selected;
    public ComputerBrowserViewModel(IComputerBrowseService? browser = null, IExternalDocumentService? documents = null, IExternalEditorDialogs? dialogs = null,
        IExternalFileOperationService? operations = null, IExternalOperationDialogs? operationDialogs = null)
        : base("Файлы компьютера", "Общий просмотр доступных папок Windows", "Icon.Files")
    {
        _browser = browser; _documents = documents; _dialogs = dialogs; _operations = operations; _operationDialogs = operationDialogs;
        View = CollectionViewSource.GetDefaultView(Items);
        View.Filter = item => item is ComputerListItem row && row.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        GoCommand = new AsyncRelayCommand(_ => NavigateAsync(Address), () => Ready);
        RefreshCommand = new AsyncRelayCommand(_ => NavigateAsync(_path), () => Ready);
        ComputerCommand = new AsyncRelayCommand(_ => NavigateAsync(""), () => Ready);
        ParentCommand = new AsyncRelayCommand(_ => NavigateAsync(_parent!), () => Ready && _parent is not null);
        OpenCommand = new AsyncRelayCommand(_ => OpenAsync(), () => Ready && Selected is { } item &&
            (item.Data.CanEnter || _documents is not null && _dialogs is not null && !item.Data.IsDirectory && item.Note == "" && DocumentValidator.Supports(item.Name)));
        RecoverCommand = new AsyncRelayCommand(_ => RecoverAsync(), () => Ready && _documents is not null && Selected is { Data.IsDirectory: false, Note: "" } item && DocumentValidator.Supports(item.Name));
        CreateFileCommand = new AsyncRelayCommand(_ => ChangeAsync(ExternalChange.CreateFile), () => CanChange);
        CreateFolderCommand = new AsyncRelayCommand(_ => ChangeAsync(ExternalChange.CreateDirectory), () => CanChange);
        CopyCommand = new AsyncRelayCommand(_ => ChangeAsync(ExternalChange.Copy), () => CanChange && CanSelect);
        MoveCommand = new AsyncRelayCommand(_ => ChangeAsync(ExternalChange.Move), () => CanChange && CanSelect);
        RenameCommand = new AsyncRelayCommand(_ => ChangeAsync(ExternalChange.Move, true), () => CanChange && CanSelect);
        RecoverOperationsCommand = new AsyncRelayCommand(_ => RecoverOperationsAsync(), () => Ready && _operations is not null);
    }
    private bool Ready => _browser is not null && CurrentUser is not null && !IsLoading;
    public ObservableCollection<ComputerListItem> Items { get; } = [];
    public ICollectionView View { get; }
    public string Address { get => _address; set => SetProperty(ref _address, value); }
    public string SearchText { get => _search; set { if (SetProperty(ref _search, value)) View.Refresh(); } }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public ComputerListItem? Selected { get => _selected; set { SetProperty(ref _selected, value); CommandManager.InvalidateRequerySuggested(); } }
    public ICommand GoCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ComputerCommand { get; }
    public ICommand ParentCommand { get; }
    public ICommand OpenCommand { get; }
    public ICommand RecoverCommand { get; }
    public ICommand CreateFileCommand { get; }
    public ICommand CreateFolderCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand MoveCommand { get; }
    public ICommand RenameCommand { get; }
    public ICommand RecoverOperationsCommand { get; }
    private bool CanChange => Ready && _path != "" && _operations is not null && _operationDialogs is not null;
    private bool CanSelect => Selected is { Note: "" } && (!Selected.Data.IsDirectory || Selected.Data.CanEnter);
    private async Task ChangeAsync(ExternalChange kind, bool rename = false)
    {
        if (!CanChange || kind is ExternalChange.Copy or ExternalChange.Move && !CanSelect) return;
        var generation = _generation; var selected = Selected; var actor = CurrentUser?.Id;
        var destination = kind is ExternalChange.Copy or ExternalChange.Move && !rename ? _operationDialogs!.ChooseDirectory(_path) : _path;
        if (destination is null || generation != _generation) return;
        var name = _operationDialogs!.AskName(rename ? "Новое имя" : kind == ExternalChange.CreateDirectory ? "Имя новой папки" : "Имя файла или копии (с расширением)",
            selected is not null && kind is ExternalChange.Copy or ExternalChange.Move ? selected.Name : kind == ExternalChange.CreateDirectory ? "Новая папка" : "Новый.txt");
        if (name is null || generation != _generation) return;
        var request = new ExternalChangeRequest(kind, kind is ExternalChange.Copy or ExternalChange.Move ? selected!.Data.Path : null, destination, name);
        if (kind is ExternalChange.Copy or ExternalChange.Move && !_operationDialogs.Confirm(request) || generation != _generation) return;
        IsLoading = true; ErrorMessage = null; CommandManager.InvalidateRequerySuggested();
        try
        {
            var result = await _operations!.ExecuteAsync(request);
            if (generation != _generation) return;
            if (!result.Succeeded)
            {
                ErrorMessage = result.Status == FileStatus.Unsupported ? "Перемещение между томами пока недоступно. Используйте копирование; исходник останется на месте." :
                    result.Status == FileStatus.RecoveryRequired ? "Есть незавершённая файловая операция. Нажмите «Проверить операции»." :
                    result.Status == FileStatus.LimitExceeded ? "Лимит операции: 1000 объектов, 100 МиБ на файл, 1 ГиБ суммарно." : result.Message;
                return;
            }
            IsLoading = false; await NavigateAsync(_path);
            if (CurrentUser?.Id == actor) Status = "Операция выполнена. " + Status;
        }
        catch { if (generation == _generation) ErrorMessage = "Файловая операция не подтверждена. Проверьте доступность диска и аудита."; }
        finally { if (generation == _generation) IsLoading = false; CommandManager.InvalidateRequerySuggested(); }
    }
    private async Task RecoverOperationsAsync()
    {
        if (!Ready || _operations is null) return;
        var generation = _generation; IsLoading = true;
        try
        {
            var result = await _operations.RecoverAsync();
            if (generation == _generation) ErrorMessage = result.Succeeded ? "Проверка незавершённых операций выполнена. Обновите список." : result.Message;
        }
        catch { if (generation == _generation) ErrorMessage = "Восстановление не подтверждено. Служебные данные сохранены для проверки."; }
        finally { if (generation == _generation) IsLoading = false; CommandManager.InvalidateRequerySuggested(); }
    }
    public async Task OpenAsync()
    {
        if (!Ready || Selected is not { } selected) return;
        if (selected.Data.CanEnter) { await NavigateAsync(selected.Data.Path); return; }
        if (_documents is null || _dialogs is null || selected.Data.IsDirectory || selected.Note != "" || !DocumentValidator.Supports(selected.Name)) return;
        var generation = _generation; IsLoading = true; ErrorMessage = null;
        try
        {
            var result = await _documents.OpenAsync(selected.Data.Path);
            if (generation != _generation || CurrentUser is null) return;
            if (result.Succeeded) _dialogs.OpenExternalEditor(result.Value!);
            else ErrorMessage = result.Status == FileStatus.RecoveryRequired ? "Есть незавершённая внешняя операция. Сначала «Действия → Проверить операции», затем при необходимости «Восстановить» для выбранного документа." : result.Message;
        }
        catch { if (generation == _generation) ErrorMessage = "Открытие не подтверждено. Проверьте файл, базу данных и подготовку схемы."; }
        finally { if (generation == _generation) IsLoading = false; CommandManager.InvalidateRequerySuggested(); }
    }
    private async Task RecoverAsync()
    {
        if (!Ready || _documents is null || Selected is not { } item) return;
        var generation = _generation; IsLoading = true;
        try
        {
            var result = await _documents.RecoverAsync(item.Data.Path);
            if (generation == _generation) ErrorMessage = result.Succeeded ? "Восстановление проверено. Обновите список и откройте файл." : result.Message;
        }
        catch { if (generation == _generation) ErrorMessage = "Восстановление не подтверждено. Резервные данные сохранены для проверки."; }
        finally { if (generation == _generation) IsLoading = false; CommandManager.InvalidateRequerySuggested(); }
    }
    public override void ResetUserState()
    {
        _generation++; _pending?.Cancel(); _pending = null;
        Items.Clear(); Selected = null; _path = ""; _parent = null; Address = ""; SearchText = "";
        Status = "Выберите диск или укажите путь к локальной папке.";
        base.ResetUserState();
    }
    public override Task ActivateAsync() => NavigateAsync(_path);
    public async Task NavigateAsync(string path)
    {
        if (!Ready) return;
        var generation = ++_generation;
        using var pending = new CancellationTokenSource(); _pending = pending;
        IsLoading = true; Items.Clear(); Selected = null; ErrorMessage = null; Status = "Загрузка…";
        CommandManager.InvalidateRequerySuggested();
        try
        {
            var result = await _browser!.ListAsync(path, pending.Token);
            if (generation != _generation || CurrentUser is null) return;
            if (!result.Succeeded)
            {
                ErrorMessage = result.Status == FileStatus.Denied ?
                    "Просмотр запрещён: проверьте сеанс и права Windows. Личный Storage открывается через «Мои файлы»." :
                    result.Status == FileStatus.Invalid ? "Укажите абсолютный путь к локальной папке, например C:\\Users, без .. и ссылок." : result.Message;
                Status = "Список не загружен."; return;
            }
            var directory = result.Value!;
            _path = directory.Path; _parent = directory.ParentPath; Address = directory.Path;
            foreach (var entry in directory.Entries) Items.Add(new(entry));
            Status = directory.IsTruncated ? "Показаны первые 10000 объектов. Используйте более узкую папку." :
                Items.Count == 0 ? "Папка пуста." : $"Объектов: {Items.Count}. Файлы доступны всем пользователям приложения по правам Windows.";
        }
        catch (OperationCanceledException) { }
        catch { if (generation == _generation) { ErrorMessage = "Не удалось прочитать папку. Проверьте доступность диска, сеанс и аудит."; Status = "Список не загружен."; } }
        finally
        {
            if (ReferenceEquals(_pending, pending)) _pending = null;
            if (generation == _generation) { IsLoading = false; CommandManager.InvalidateRequerySuggested(); }
        }
    }
}
