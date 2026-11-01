using System.Windows;
using System.Windows.Input;
using SecureFileManager.Wpf.ViewModels;
namespace SecureFileManager.Wpf.Views;

// Only native window behaviour belongs here; navigation and state live in ViewModels.
public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel) { InitializeComponent(); DataContext = viewModel; }
    private void MinimizeExecuted(object sender, ExecutedRoutedEventArgs e) => SystemCommands.MinimizeWindow(this);
    private void MaximizeExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized) SystemCommands.RestoreWindow(this);
        else SystemCommands.MaximizeWindow(this);
    }
    private void CloseExecuted(object sender, ExecutedRoutedEventArgs e) => SystemCommands.CloseWindow(this);
}
