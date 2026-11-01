using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SecureFileManager.Wpf.ViewModels;
using SecureFileManager.Wpf.Views;
using SecureFileManager.Data.DependencyInjection;

namespace SecureFileManager.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            _services = new ServiceCollection()
                .AddSecureFileManagerData()
                .AddSingleton<DrivesViewModel>()
                .AddSingleton<FilesViewModel>()
                .AddSingleton<ArchivesViewModel>()
                .AddSingleton<FormatsViewModel>()
                .AddSingleton<JournalViewModel>()
                .AddSingleton<SettingsViewModel>()
                .AddSingleton<MainWindowViewModel>()
                .AddSingleton<MainWindow>()
                .BuildServiceProvider(new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true
                });
        }
        catch (InvalidOperationException)
        {
            MessageBox.Show("Не удалось настроить приложение. Проверьте локальную конфигурацию базы данных и окружение.",
                "Ошибка конфигурации", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }
        MainWindow = _services.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }
}

