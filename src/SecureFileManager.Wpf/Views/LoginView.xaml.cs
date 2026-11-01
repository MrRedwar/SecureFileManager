using System.Windows;
using System.Windows.Controls;
using SecureFileManager.Wpf.ViewModels;
namespace SecureFileManager.Wpf.Views;
public partial class LoginView : UserControl
{
    public LoginView() { InitializeComponent(); Unloaded += (_, _) => Password.Clear(); }
    private void Submit(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel model && model.SubmitCommand.CanExecute(null))
        {
            var input = new PasswordInput(Password.Password);
            Password.Clear();
            model.SubmitCommand.Execute(input);
        }
    }
}
