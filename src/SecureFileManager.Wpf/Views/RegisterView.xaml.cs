using System.Windows;
using System.Windows.Controls;
using SecureFileManager.Wpf.ViewModels;
namespace SecureFileManager.Wpf.Views;
public partial class RegisterView : UserControl
{
    public RegisterView() { InitializeComponent(); Unloaded += (_, _) => { Password.Clear(); Confirmation.Clear(); }; }
    private void Submit(object sender, RoutedEventArgs e)
    {
        if (DataContext is RegisterViewModel model && model.SubmitCommand.CanExecute(null))
        {
            var input = new PasswordInput(Password.Password, Confirmation.Password);
            Password.Clear(); Confirmation.Clear();
            model.SubmitCommand.Execute(input);
        }
    }
}
