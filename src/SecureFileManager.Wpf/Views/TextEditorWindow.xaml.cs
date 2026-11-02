using System.ComponentModel;
using System.Windows;
using SecureFileManager.Wpf.ViewModels;
namespace SecureFileManager.Wpf.Views;
public partial class TextEditorWindow : Window
{
    private readonly TextEditorViewModel _model; private bool _allowClose, _closing;
    public TextEditorWindow(TextEditorViewModel model)
    {
        InitializeComponent(); DataContext = _model = model; Closing += OnClosing;
        model.SessionEnded += (_, _) => { _allowClose = true; Close(); };
    }
    public async Task<bool> TryCloseAsync()
    {
        if (_closing) return false;
        _closing = true;
        try { if (!await _model.CanCloseAsync()) return false; _allowClose = true; Close(); return true; }
        finally { _closing = false; }
    }
    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose) return;
        e.Cancel = true;
        await Dispatcher.InvokeAsync(async () => await TryCloseAsync()).Task.Unwrap();
    }
}
