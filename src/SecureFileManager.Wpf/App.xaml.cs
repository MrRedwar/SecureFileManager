using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SecureFileManager.Core.Interfaces;
using SecureFileManager.Core.Security;
using SecureFileManager.Core.Services;
using SecureFileManager.Data.DependencyInjection;
using SecureFileManager.Wpf.Infrastructure.FileSystem;
using SecureFileManager.Wpf.ViewModels;
using SecureFileManager.Wpf.Views;
using SecureFileManager.Core.Files;
using SecureFileManager.Wpf.Infrastructure.Dialogs;
namespace SecureFileManager.Wpf;

public partial class App : Application
{
    private ServiceProvider? _services;
    private AuthenticationWindow? _authentication;
    private Views.MainWindow? _shell;
    private bool _transition;
    private bool _allowShellClose;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        try
        {
            var services = CreateServices();
            _services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
            ShowLogin("");
        }
        catch
        {
            MessageBox.Show("Не удалось настроить приложение. Проверьте конфигурацию базы данных и личного хранилища.",
                "Ошибка конфигурации", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
    public static IServiceCollection CreateServices()
    {
        return new ServiceCollection().AddSecureFileManagerData()
                .AddSingleton<IClock, SystemClock>()
                .AddSingleton<IPasswordSecurity>(_ => new PasswordSecurity())
                .AddSingleton(_ => new LoginProtectionPolicy())
                .AddSingleton<UserSession>()
                .AddSingleton<IUserSession>(s => s.GetRequiredService<UserSession>())
                .AddSingleton<PersonalStorage>(_ => PersonalStorage.FromConfiguration())
                .AddSingleton<IPersonalStorageProvisioner>(s => s.GetRequiredService<PersonalStorage>())
                .AddSingleton<IPathPolicy>(s => s.GetRequiredService<PersonalStorage>())
                .AddSingleton<IPhysicalFileStorage, WindowsFileStorage>()
                .AddSingleton<FileService>()
                .AddSingleton<IFileService>(s => s.GetRequiredService<FileService>())
                .AddSingleton<IArchiveService>(s => s.GetRequiredService<FileService>())
                .AddSingleton<IDriveService, WindowsDriveService>()
                .AddSingleton<WindowsFileDialogs>()
                .AddSingleton<IFileDialogs>(s => s.GetRequiredService<WindowsFileDialogs>())
                .AddSingleton<IArchiveDialogs>(s => s.GetRequiredService<WindowsFileDialogs>())
                .AddSingleton<IFileAccessPolicy, PersonalFileAccessPolicy>()
                .AddSingleton<IAuditService, SecurityAudit>()
                .AddSingleton<IAuthenticationService, AuthenticationService>()
                .AddSingleton<IRegistrationService, RegistrationService>()
                .AddSingleton<IUserStorageService, UserStorageService>()
                .AddSingleton<DrivesViewModel>().AddSingleton<FilesViewModel>()
                .AddSingleton<ArchivesViewModel>().AddSingleton<FormatsViewModel>()
                .AddSingleton<JournalViewModel>().AddSingleton<SettingsViewModel>()
                .AddSingleton<MainWindowViewModel>()
                .AddTransient<Views.MainWindow>()
                .AddTransient<AuthenticationWindowViewModel>().AddTransient<AuthenticationWindow>();
    }
    private void ShowLogin(string message)
    {
        _authentication = _services!.GetRequiredService<AuthenticationWindow>();
        var model = (AuthenticationWindowViewModel)_authentication.DataContext;
        model.Login.Message = message;
        model.Authenticated += async (_, _) => await OpenShellAsync();
        _authentication.Closed += (_, _) => { if (!_transition && _shell is null) Shutdown(); };
        MainWindow = _authentication;
        _authentication.Show();
        _ = model.CheckConnectionAsync();
    }
    private async Task OpenShellAsync()
    {
        if (_transition || _authentication is null) return;
        _transition = true;
        _authentication.IsEnabled = false;
        try
        {
            var root = await _services!.GetRequiredService<IUserStorageService>().PrepareCurrentAsync();
            if (!root.Succeeded)
            {
                await _services!.GetRequiredService<IAuthenticationService>().LogoutAsync();
                ((AuthenticationWindowViewModel)_authentication.DataContext).Login.Message =
                    "Личное хранилище недоступно. Вход завершён без открытия защищённых разделов.";
                return;
            }
            _shell = _services!.GetRequiredService<Views.MainWindow>();
            _shell.Closing += ShellClosing;
            _services!.GetRequiredService<MainWindowViewModel>().LogoutRequested += LogoutRequested;
            MainWindow = _shell;
            _shell.Show();
            _authentication.Close();
            _authentication = null;
            await _services!.GetRequiredService<MainWindowViewModel>().ActivateCurrentAsync();
        }
        catch
        {
            await _services!.GetRequiredService<IAuthenticationService>().LogoutAsync();
            if (_authentication is not null) ((AuthenticationWindowViewModel)_authentication.DataContext).Login.Message =
                "Не удалось открыть рабочую область. Сеанс завершён.";
        }
        finally { if (_authentication is not null) _authentication.IsEnabled = true; _transition = false; }
    }
    private async void LogoutRequested(object? sender, EventArgs e) => await EndSessionAsync(false);
    private async void ShellClosing(object? sender, CancelEventArgs e)
    {
        if (_allowShellClose) return;
        e.Cancel = true;
        await EndSessionAsync(true);
    }
    private async Task EndSessionAsync(bool exit)
    {
        if (_transition) return;
        _transition = true;
        string message;
        if (_shell is not null) _shell.IsEnabled = false;
        if (!await _services!.GetRequiredService<IFileDialogs>().CloseEditorsAsync())
        { if (_shell is not null) _shell.IsEnabled = true; _transition = false; return; }
        await _services!.GetRequiredService<IFileService>().WaitForIdleAsync();
        try
        {
            var result = await _services!.GetRequiredService<IAuthenticationService>().LogoutAsync();
            message = result.Message;
        }
        catch { message = "Сеанс завершён. Запись выхода в журнал не подтверждена."; }
        finally
        {
            _services!.GetRequiredService<MainWindowViewModel>().LogoutRequested -= LogoutRequested;
            _allowShellClose = true;
            _shell?.Close();
            _shell = null;
            _allowShellClose = false;
            _transition = false;
        }
        if (exit) Shutdown(); else ShowLogin(message);
    }
    protected override void OnExit(ExitEventArgs e) { _services?.Dispose(); base.OnExit(e); }
}
