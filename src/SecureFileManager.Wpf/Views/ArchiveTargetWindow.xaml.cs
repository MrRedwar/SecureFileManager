using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SecureFileManager.Wpf.ViewModels;
namespace SecureFileManager.Wpf.Views;
public partial class ArchiveTargetWindow : Window
{
    private readonly ArchiveTargetViewModel _model;
    public ArchiveTargetWindow(ArchiveTargetViewModel model)
    {
        InitializeComponent(); DataContext = _model = model;
        Loaded += async (_, _) => await model.Browser.LoadAsync(model.InitialDirectory);
    }
    private void OpenFolder(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGrid grid && e.OriginalSource is DependencyObject source &&
            ItemsControl.ContainerFromElement(grid, source) is DataGridRow && _model.Browser.OpenCommand.CanExecute(null))
            _model.Browser.OpenCommand.Execute(null);
    }
    private void Accept(object sender, RoutedEventArgs e)
    { if (_model.CanAccept) DialogResult = true; }
}
