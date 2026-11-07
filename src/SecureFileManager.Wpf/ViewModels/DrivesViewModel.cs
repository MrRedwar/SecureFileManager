using System.Collections.ObjectModel;
using System.Windows.Input;
using SecureFileManager.Core.Files;
using SecureFileManager.Core.Interfaces;
namespace SecureFileManager.Wpf.ViewModels;
public sealed record DriveCard(DriveSnapshot Data)
{
    public string Name => Data.Name;
    public string Label => string.IsNullOrWhiteSpace(Data.Label) ? "Без метки" : Data.Label;
    public string Type => Data.Type switch
    { "Fixed" => "Локальный диск", "Removable" => "Съёмный", "CDRom" => "Оптический", "Network" => "Сетевой", "Ram" => "RAM-диск", _ => Data.Type };
    public string Format => Data.Format;
    public string State => Data.IsReady ? "Готов" : "Не готов / недоступен";
    public string Total => Data.IsReady ? FileDisplay.Bytes(Data.Total) : "—";
    public string Free => Data.IsReady ? FileDisplay.Bytes(Data.Free) : "—";
    public string Used => Data.IsReady ? FileDisplay.Bytes(Data.Used) : "—";
    public double Percent => Data.Percent;
}
public sealed class DrivesViewModel : SectionViewModel
{
    private readonly IDriveService? _drives; private long _generation;
    public ObservableCollection<DriveCard> Items { get; } = [];
    public ICommand RefreshCommand { get; }
    public ICommand BrowseDriveCommand { get; }
    public ComputerBrowserViewModel Computer { get; }
    private int _selectedTab;
    public int SelectedTab
    {
        get => _selectedTab;
        set { if (SetProperty(ref _selectedTab, value) && value == 1) _ = Computer.ActivateAsync(); }
    }
    public DrivesViewModel(IDriveService? drives = null, ComputerBrowserViewModel? computer = null)
        : base("Диски", "Накопители и общие файлы компьютера", "Icon.Drives")
    {
        _drives = drives; Computer = computer ?? new();
        RefreshCommand = new AsyncRelayCommand(_ => ActivateAsync(), () => _drives is not null && CurrentUser is not null && !IsLoading);
        BrowseDriveCommand = new AsyncRelayCommand(async value =>
        {
            if (value is not DriveCard drive) return;
            // Set backing state here to avoid launching two concurrent navigations.
            SetProperty(ref _selectedTab, 1, nameof(SelectedTab));
            await Computer.NavigateAsync(drive.Name);
        }, () => _drives is not null && CurrentUser is not null && !Computer.IsLoading,
            value => value is DriveCard { Data.IsReady: true } drive && drive.Data.Type != "Network");
    }
    public override void ResetUserState() { _generation++; Items.Clear(); Computer.ResetUserState(); _selectedTab = 0; RaisePropertyChanged(nameof(SelectedTab)); base.ResetUserState(); }
    public override void UpdateSession(SessionIdentity? identity) { base.UpdateSession(identity); Computer.UpdateSession(identity); }
    public override async Task ActivateAsync()
    {
        if (SelectedTab == 1) { await Computer.ActivateAsync(); return; }
        if (_drives is null || CurrentUser is null || IsLoading) return;
        var generation = _generation; IsLoading = true; ErrorMessage = null;
        try
        {
            var result = await _drives.GetAsync();
            if (generation != _generation) return;
            Items.Clear();
            if (result.Succeeded) foreach (var drive in result.Value!) Items.Add(new(drive));
            else ErrorMessage = result.Message;
        }
        catch { if (generation == _generation) ErrorMessage = "Не удалось получить характеристики накопителей."; }
        finally { if (generation == _generation) IsLoading = false; CommandManager.InvalidateRequerySuggested(); }
    }
}
