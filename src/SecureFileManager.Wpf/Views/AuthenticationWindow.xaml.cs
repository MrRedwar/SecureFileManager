using System.Windows;
using SecureFileManager.Wpf.ViewModels;
namespace SecureFileManager.Wpf.Views;
public partial class AuthenticationWindow : Window
{
    public AuthenticationWindow(AuthenticationWindowViewModel model) { InitializeComponent(); DataContext = model; }
}
