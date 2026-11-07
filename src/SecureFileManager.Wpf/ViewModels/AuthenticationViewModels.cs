using System.Windows.Input;
using SecureFileManager.Core.Interfaces;
using SecureFileManager.Core.Results;
namespace SecureFileManager.Wpf.ViewModels;

public sealed class PasswordInput(string password, string confirmation = "")
{
    public string Password { get; private set; } = password;
    public string Confirmation { get; private set; } = confirmation;
    public void Clear() { Password = string.Empty; Confirmation = string.Empty; }
    public override string ToString() => "[Password input omitted]";
}
public sealed class AsyncRelayCommand(Func<object?, Task> execute, Func<bool> canExecute, Predicate<object?>? parameterAllowed = null) : ICommand
{
    private bool _running;
    public bool CanExecute(object? parameter) => !_running && canExecute() && (parameterAllowed?.Invoke(parameter) ?? true);
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) { (parameter as PasswordInput)?.Clear(); return; }
        _running = true; CommandManager.InvalidateRequerySuggested();
        try { await execute(parameter); }
        finally { _running = false; CommandManager.InvalidateRequerySuggested(); }
    }
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
public abstract class AuthenticationFormViewModel : ObservableObject
{
    private string _username = "";
    public string Username { get => _username; set => SetProperty(ref _username, value); }
    private bool _messageIsError = true;
    public bool MessageIsError { get => _messageIsError; set => SetProperty(ref _messageIsError, value); }
    private string _message = "";
    public string Message { get => _message; set => SetProperty(ref _message, value); }
    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        protected set { if (SetProperty(ref _isBusy, value)) { RaisePropertyChanged(nameof(CanSubmit)); CommandManager.InvalidateRequerySuggested(); } }
    }
    public bool CanSubmit => !IsBusy;
    public ICommand SubmitCommand { get; protected set; } = null!;
}
public sealed class LoginViewModel : AuthenticationFormViewModel
{
    private readonly IAuthenticationService _authentication;
    public event EventHandler? Authenticated;
    public LoginViewModel(IAuthenticationService authentication)
    {
        _authentication = authentication;
        SubmitCommand = new AsyncRelayCommand(SubmitAsync, () => CanSubmit);
    }
    public async Task SubmitAsync(object? value)
    {
        if (value is not PasswordInput input) return;
        if (IsBusy) { input.Clear(); return; }
        IsBusy = true; Message = ""; MessageIsError = true;
        try
        {
            var pending = _authentication.LoginAsync(Username, input.Password);
            input.Clear();
            var result = await pending;
            Message = result.Message; MessageIsError = !result.Succeeded;
            if (result.Succeeded) Authenticated?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException) { Message = "Операция отменена."; }
        catch { Message = "Не удалось выполнить вход. Операция не подтверждена."; }
        finally { input.Clear(); IsBusy = false; }
    }
}
public sealed class RegisterViewModel : AuthenticationFormViewModel
{
    private readonly IRegistrationService _registration;
    public event Action<string, string>? Registered;
    public RegisterViewModel(IRegistrationService registration)
    {
        _registration = registration;
        SubmitCommand = new AsyncRelayCommand(SubmitAsync, () => CanSubmit);
    }
    public async Task SubmitAsync(object? value)
    {
        if (value is not PasswordInput input) return;
        if (IsBusy) { input.Clear(); return; }
        IsBusy = true; Message = ""; MessageIsError = true;
        try
        {
            var pending = _registration.RegisterAsync(Username, input.Password, input.Confirmation);
            input.Clear();
            var result = await pending;
            Message = result.Message;
            if (result.Succeeded) Registered?.Invoke(Username, result.Message);
        }
        catch (OperationCanceledException) { Message = "Операция отменена."; }
        catch { Message = "Результат регистрации не подтверждён. Проверьте подключение; перед повтором попробуйте войти."; }
        finally { input.Clear(); IsBusy = false; }
    }
}
public sealed class AuthenticationWindowViewModel : ObservableObject
{
    private readonly IDatabaseConnectionCheck? _connection;
    private bool _checkingConnection;
    private string _connectionStatus = "Соединение с БД не проверено";
    public string ConnectionStatus { get => _connectionStatus; private set => SetProperty(ref _connectionStatus, value); }
    public ICommand RetryConnectionCommand { get; }
    public LoginViewModel Login { get; }
    public RegisterViewModel Register { get; }
    private AuthenticationFormViewModel _page;
    public AuthenticationFormViewModel Page { get => _page; private set { if (SetProperty(ref _page, value)) RaisePropertyChanged(nameof(Title)); } }
    public string Title => Page is LoginViewModel ? "Вход в хранилище" : "Создать учётную запись";
    public ICommand ShowLoginCommand { get; }
    public ICommand ShowRegisterCommand { get; }
    public event EventHandler? Authenticated;
    public AuthenticationWindowViewModel(IAuthenticationService authentication, IRegistrationService registration,
        IDatabaseConnectionCheck? connection = null)
    {
        _connection = connection;
        Login = new(authentication); Register = new(registration); _page = Login;
        RetryConnectionCommand = new AsyncRelayCommand(_ => CheckConnectionAsync(), () => !_checkingConnection && !Page.IsBusy);
        ShowLoginCommand = new RelayCommand(_ => Page = Login, _ => !Page.IsBusy);
        ShowRegisterCommand = new RelayCommand(_ => Page = Register, _ => !Page.IsBusy);
        Login.Authenticated += (_, _) => Authenticated?.Invoke(this, EventArgs.Empty);
        Register.Registered += (username, message) => { Login.Username = username; Login.Message = message; Login.MessageIsError = false; Page = Login; };
    }
    public async Task CheckConnectionAsync()
    {
        if (_checkingConnection) return;
        _checkingConnection = true;
        ConnectionStatus = "Проверка соединения с БД…";
        try
        {
            ConnectionStatus = _connection is not null && await _connection.CanConnectAsync()
                ? "Подключение к БД проверено. Вход требует проверки пароля и аудита."
                : "БД недоступна. Повторите проверку или закройте окно. Вход запрещён.";
        }
        catch { ConnectionStatus = "БД недоступна. Повторите проверку или закройте окно. Вход запрещён."; }
        finally { _checkingConnection = false; CommandManager.InvalidateRequerySuggested(); }
    }
}
