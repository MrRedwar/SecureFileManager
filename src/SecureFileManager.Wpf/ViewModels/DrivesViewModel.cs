using System.Collections.ObjectModel;
using System.Windows.Input;
using SecureFileManager.Core.Files;
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
    public DrivesViewModel(IDriveService? drives = null) : base("Диски", "Обзор накопителей · только чтение", "Icon.Drives")
    {
        _drives = drives;
        RefreshCommand = new AsyncRelayCommand(_ => ActivateAsync(), () => _drives is not null && CurrentUser is not null && !IsLoading);
    }
    public override void ResetUserState() { _generation++; Items.Clear(); base.ResetUserState(); }
    public override async Task ActivateAsync()
    {
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
