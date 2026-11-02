using System.Windows;
using SecureFileManager.Core.Files;
namespace SecureFileManager.Wpf.Views;
public partial class ArchivePreviewWindow : Window
{
    public ArchivePreviewWindow(ArchivePreview preview, bool confirmExtraction)
    {
        InitializeComponent(); DataContext = preview;
        ContinueButton.Visibility = confirmExtraction ? Visibility.Visible : Visibility.Collapsed;
    }
    private void Accept(object sender, RoutedEventArgs e) => DialogResult = true;
}
