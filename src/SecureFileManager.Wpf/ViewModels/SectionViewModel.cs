using SecureFileManager.Core.Interfaces;
using System.Windows.Input;
namespace SecureFileManager.Wpf.ViewModels;

public class SectionViewModel(string title, string description, string iconKey = "Icon.Files") : ObservableObject
{
    public string Title { get; } = title;
    public string Description { get; } = description;
    public string IconKey { get; } = iconKey;
    public SessionIdentity? CurrentUser { get; private set; }
    public string SessionStatus => CurrentUser?.Username ?? "Не авторизован";
    public virtual void ResetUserState() { IsLoading = false; ErrorMessage = null; }
    public virtual Task ActivateAsync() => Task.CompletedTask;
    public void UpdateSession(SessionIdentity? identity)
    { ResetUserState(); CurrentUser = identity; RaisePropertyChanged(nameof(CurrentUser)); RaisePropertyChanged(nameof(SessionStatus)); RaisePropertyChanged("Breadcrumb"); RaisePropertyChanged("StorageStatus"); RaisePropertyChanged("DatabaseStatus"); }
    public string UnavailableReason => "Действие станет доступно после реализации сервиса и входа в приложение.";
    public ICommand UnavailableCommand { get; } = new RelayCommand(_ => { }, _ => false);
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    private string? _errorMessage;
    public string? ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
}
