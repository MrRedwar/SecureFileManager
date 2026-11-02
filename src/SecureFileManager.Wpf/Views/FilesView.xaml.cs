using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SecureFileManager.Wpf.ViewModels;
namespace SecureFileManager.Wpf.Views;
public partial class FilesView : UserControl
{
    public FilesView() { InitializeComponent(); }
    private void SelectionChanged(object sender, SelectionChangedEventArgs e)
    { if (sender is DataGrid grid && DataContext is FilesViewModel model) model.SetSelection(grid.SelectedItems.Cast<FileListItem>()); }
    private void OpenDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGrid grid && e.OriginalSource is DependencyObject source &&
            ItemsControl.ContainerFromElement(grid, source) is DataGridRow && DataContext is FilesViewModel model &&
            model.OpenCommand.CanExecute(null)) model.OpenCommand.Execute(null);
    }
}
