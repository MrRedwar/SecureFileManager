using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using SecureFileManager.Core.Entities;
using SecureFileManager.Core.Files;

namespace SecureFileManager.Wpf.ViewModels;

public sealed record FileListItem(string Name, string Type, string Size, string ModifiedAt, string IconKey)
{
    public FileEntry? Entry { get; init; }
    public long SizeBytes => Entry?.SizeBytes ?? 0;
    public DateTime ModifiedUtc => Entry?.ModifiedUtc ?? DateTime.MinValue;
    public string State => Entry?.State switch
    {
        EntryState.Managed => "Учтён", EntryState.Untracked => "Вне БД — сверить",
        EntryState.Changed => "Изменён — сверить", EntryState.Missing => "Отсутствует — сверить", _ => "Недоступно"
    };
}
public sealed class FilesViewModel : SectionViewModel
{
    private sealed class FileItems : ObservableCollection<FileListItem>
    {
        public void Replace(IReadOnlyList<FileListItem> items)
        {
            CheckReentrancy();
            Items.Clear();
            foreach (var item in items) Items.Add(item);
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(
                System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }
    }
    private readonly IFileService? _files;
    private readonly IFileDialogs? _dialogs;
    private long _generation;
    private string _directory = "", _search = "";
    private int _sort;
    private bool _descending;
    private bool _refreshQueued;
    private readonly System.Windows.Threading.Dispatcher? _dispatcher;
    public ObservableCollection<FileListItem> Items { get; } = new FileItems();
    public ICollectionView View { get; }
    private FileListItem? _selectedFile;
    public FileListItem? SelectedFile
    {
        get => _selectedFile;
        set { if (SetProperty(ref _selectedFile, value)) { RaisePropertyChanged(nameof(SelectedCount)); CommandManager.InvalidateRequerySuggested(); } }
    }
    public int SelectedCount => SelectedFile is null ? 0 : 1;
    public string Breadcrumb => CurrentUser is null ? "Моё хранилище / Вход не выполнен" :
        "Моё хранилище / " + (_directory == "" ? "Корень" : _directory.Replace("/", " / "));
    public string CurrentDirectory => _directory;
    public string EmptyText => string.IsNullOrEmpty(_search) ? "В этой папке пока нет объектов." : "Ничего не найдено по текущему фильтру.";
    public string SearchText { get => _search; set { if (SetProperty(ref _search, value)) { View.Refresh(); RaisePropertyChanged(nameof(EmptyText)); } } }
    public IReadOnlyList<string> SortOptions { get; } = ["Имя", "Тип", "Размер", "Дата изменения"];
    public int SortIndex { get => _sort; set { if (SetProperty(ref _sort, value)) Sort(); } }
    public bool Descending { get => _descending; set { if (SetProperty(ref _descending, value)) Sort(); } }
    public ICommand RefreshCommand { get; }
    public ICommand ParentCommand { get; }
    public ICommand RootCommand { get; }
    public ICommand CreateFileCommand { get; }
    public ICommand CreateFolderCommand { get; }
    public ICommand OpenCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand SynchronizeCommand { get; }
    public ICommand RecoverCommand { get; }
    private bool Ready => _files is not null && CurrentUser is not null && !IsLoading;
    public FilesViewModel(IFileService? files = null, IFileDialogs? dialogs = null, bool structuredOnly = false)
        : base("Мои файлы", "Личное пространство для ваших документов", "Icon.Files")
    {
        _files = files; _dialogs = dialogs; View = CollectionViewSource.GetDefaultView(Items);
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        _dispatcher = dispatcher?.CheckAccess() == true ? dispatcher : null;
        if (_files is not null) _files.Changed += OnFilesChanged;
        View.Filter = value => value is FileListItem item && item.Name.Contains(_search, StringComparison.CurrentCultureIgnoreCase) &&
            (!structuredOnly || item.Entry?.Kind == FileKind.Directory || item.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
             item.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
        RefreshCommand = new AsyncRelayCommand(_ => LoadAsync(_directory), () => Ready);
        ParentCommand = new AsyncRelayCommand(_ => LoadAsync(FileRules.Parent(_directory)), () => Ready && _directory != "");
        RootCommand = new AsyncRelayCommand(_ => LoadAsync(""), () => Ready);
        CreateFileCommand = new AsyncRelayCommand(_ => CreateAsync(false), () => Ready && _dialogs is not null);
        CreateFolderCommand = new AsyncRelayCommand(_ => CreateAsync(true), () => Ready && _dialogs is not null);
        OpenCommand = new AsyncRelayCommand(_ => OpenAsync(), () => Ready && SelectedFile is not null);
        DeleteCommand = new AsyncRelayCommand(_ => DeleteAsync(), () => Ready && SelectedFile?.Entry?.Id is not null);
        ImportCommand = new AsyncRelayCommand(_ => ImportAsync(), () => Ready && _dialogs is not null);
        SynchronizeCommand = new AsyncRelayCommand(_ => SynchronizeAsync(), () => Ready && SelectedFile?.Entry is not null);
        RecoverCommand = new AsyncRelayCommand(_ => MutateAsync(() => _files!.RecoverAsync()), () => Ready);
        Sort();
    }
    private void OnFilesChanged(object? sender, FileChangeNotification e)
    {
        void Refresh()
        {
            if (CurrentUser?.Id != e.Owner) return;
            if (IsLoading) _refreshQueued = true; else _ = LoadAsync(_directory);
        }
        if (_dispatcher is not null && !_dispatcher.CheckAccess()) _dispatcher.BeginInvoke(new Action(Refresh)); else Refresh();
    }
    private void Sort()
    {
        View.SortDescriptions.Clear();
        View.SortDescriptions.Add(new(_sort switch { 1 => "Type", 2 => "SizeBytes", 3 => "ModifiedUtc", _ => "Name" },
            _descending ? ListSortDirection.Descending : ListSortDirection.Ascending));
    }
    public override void ResetUserState()
    {
        _generation++; _refreshQueued = false; base.ResetUserState(); Items.Clear(); SelectedFile = null; _directory = "";
        SearchText = ""; RaisePropertyChanged(nameof(CurrentDirectory)); RaisePropertyChanged(nameof(Breadcrumb));
    }
    public override Task ActivateAsync() => Ready ? LoadAsync(_directory) : Task.CompletedTask;
    public async Task LoadAsync(string directory)
    {
        if (!Ready) return;
        await Busy(async generation =>
        {
            var result = await _files!.ListAsync(directory);
            if (generation != _generation) return;
            SelectedFile = null;
            if (!result.Succeeded) { ErrorMessage = result.Message; return; }
            _directory = directory; RaisePropertyChanged(nameof(CurrentDirectory)); RaisePropertyChanged(nameof(Breadcrumb));
            var items = result.Value!.Select(entry => new FileListItem(entry.Name, entry.Kind == FileKind.Directory ? "Папка" :
                    (entry.Name.Contains('.') ? "Файл ." + entry.Name.Split('.')[^1] : "Файл"),
                    entry.Kind == FileKind.Directory || entry.State == EntryState.Unsafe ? "—" : FileDisplay.Bytes(entry.SizeBytes),
                    entry.State == EntryState.Unsafe ? "—" : entry.ModifiedUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss"),
                    entry.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? "Icon.Archives" : "Icon.Files") { Entry = entry }).ToArray();
            // Publish one Reset: per-item notifications inside View.DeferRefresh break WPF currency/selection.
            ((FileItems)Items).Replace(items);
        }, "Не удалось обновить список. Это не означает отмену уже выполненной операции. Нажмите «Обновить».");
    }
    private async Task Busy(Func<long, Task> action, string failureMessage = "Не удалось выполнить команду. Операция не подтверждена.")
    {
        if (IsLoading) return;
        var generation = _generation; IsLoading = true; ErrorMessage = null; CommandManager.InvalidateRequerySuggested();
        try { await action(generation); }
        catch { if (generation == _generation) ErrorMessage = failureMessage; }
        finally
        {
            if (generation == _generation)
            {
                IsLoading = false;
                if (_refreshQueued) { _refreshQueued = false; _ = LoadAsync(_directory); }
            }
            CommandManager.InvalidateRequerySuggested();
        }
    }
    private async Task MutateAsync(Func<Task<FileResult<bool>>> command)
    {
        if (!Ready) return;
        var succeeded = false; var directory = _directory;
        await Busy(async generation =>
        {
            var result = await command();
            if (generation != _generation) return;
            succeeded = result.Succeeded;
            if (!succeeded) ErrorMessage = result.Message;
        });
        if (succeeded) await LoadAsync(directory);
    }
    private Task CreateAsync(bool folder)
    {
        var name = _dialogs?.AskName(folder);
        return name is null ? Task.CompletedTask : MutateAsync(() => _files!.CreateAsync(_directory, name, folder));
    }
    private Task ImportAsync()
    {
        var choice = _dialogs?.ChooseImport();
        return choice is null ? Task.CompletedTask : MutateAsync(() => _files!.ImportAsync(_directory, choice));
    }
    private Task DeleteAsync()
    {
        var entry = SelectedFile?.Entry;
        if (entry?.Id is not Guid id || _dialogs?.Confirm("Удалить объект?",
            $"Удалить «{entry.Name}»? Каталог удаляется только если он пуст. История сохранится.") != true) return Task.CompletedTask;
        return MutateAsync(() => _files!.DeleteAsync(id));
    }
    private Task SynchronizeAsync()
    {
        var entry = SelectedFile?.Entry;
        if (entry is null || _dialogs?.Confirm("Сверить метаданные?",
            "Явно учесть выбранный объект или обновить его метаданные. Если объект отсутствует, запись будет помечена удалённой. Физические файлы не удаляются.") != true)
            return Task.CompletedTask;
        return MutateAsync(() => _files!.SynchronizeAsync(entry.RelativePath));
    }
    private async Task OpenAsync()
    {
        var entry = SelectedFile?.Entry;
        if (entry is null) return;
        if (entry.Id is null) { _dialogs?.Information("Объект не учтён в БД. Сначала явно выполните «Сверить»."); return; }
        if (entry.Kind == FileKind.Directory) { await LoadAsync(entry.RelativePath); return; }
        if (!DocumentValidator.Supports(entry.Name))
        { _dialogs?.Information($"{entry.Name}\n{FileDisplay.Bytes(entry.SizeBytes)}\nРедактор поддерживает TXT, JSON и XML в UTF-8."); return; }
        await Busy(async generation =>
        {
            var result = await _files!.ReadAsync(entry.Id.Value);
            if (generation != _generation) return;
            if (result.Succeeded) _dialogs?.OpenEditor(result.Value!); else ErrorMessage = result.Message;
        });
    }
}
