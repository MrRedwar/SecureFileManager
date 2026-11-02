using System.Windows;
namespace SecureFileManager.Wpf.Views;
public partial class NameDialog : Window
{
    public string Value => NameInput.Text;
    public NameDialog(bool folder)
    {
        InitializeComponent(); Prompt.Text = folder ? "Имя новой папки" : "Имя файла (.txt, .json или .xml)";
        NameInput.Text = folder ? "" : ".txt"; Loaded += (_, _) => { NameInput.Focus(); NameInput.Select(0, 0); };
    }
    private void Accept(object sender, RoutedEventArgs e) { DialogResult = true; }
}
