using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SecureFileManager.Wpf.ViewModels;

namespace SecureFileManager.Wpf.Views;
public partial class ComputerBrowserView : UserControl
{
    public ComputerBrowserView() => InitializeComponent();
    private void OpenFolder(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source || ItemsControl.ContainerFromElement((DataGrid)sender, source) is not DataGridRow)
            return;
        if (DataContext is ComputerBrowserViewModel model && model.OpenCommand.CanExecute(null)) model.OpenCommand.Execute(null);
    }
}
