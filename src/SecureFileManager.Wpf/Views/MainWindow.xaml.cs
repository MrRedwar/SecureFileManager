using System.Windows;
using SecureFileManager.Wpf.ViewModels;

namespace SecureFileManager.Wpf.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
