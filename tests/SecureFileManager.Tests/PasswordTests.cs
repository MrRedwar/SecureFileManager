using SecureFileManager.Core.Interfaces;
using SecureFileManager.Core.Security;
using SecureFileManager.Core.Results;
using SecureFileManager.Wpf.ViewModels;
namespace SecureFileManager.Tests;

public sealed class PasswordTests
{
    [Fact]
    public async Task BusyFormRejectsDuplicateSubmissionAndReleasesTransientPassword()
    {
        var service = new PendingAuthentication();
        var window = new AuthenticationWindowViewModel(service, service);
        var input = new PasswordInput("Test-only password!");
        var pending = window.Login.SubmitAsync(input);
        Assert.Empty(input.Password);
        Assert.True(window.Login.IsBusy);
        Assert.False(window.Login.SubmitCommand.CanExecute(null));
        Assert.False(window.ShowRegisterCommand.CanExecute(null));
        var duplicate = new PasswordInput("Second test-only password!");
        await window.Login.SubmitAsync(duplicate);
        Assert.Empty(duplicate.Password);
        Assert.Equal(1, service.Calls);
        service.Completion.SetResult(AuthOutcome.Invalid());
        await pending;
        Assert.False(window.Login.IsBusy);
        Assert.True(window.Login.CanSubmit);
        Assert.Equal("Неверный логин или пароль", window.Login.Message);
    }
    private sealed class PendingAuthentication : IAuthenticationService, IRegistrationService
    {
        public int Calls { get; private set; }
        public TaskCompletionSource<AuthOutcome> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<AuthOutcome> LoginAsync(string username, string password, CancellationToken ct = default)
        { Calls++; return Completion.Task; }
        public Task<AuthOutcome> LogoutAsync(CancellationToken ct = default) => Task.FromResult(AuthOutcome.Ok());
        public Task<AuthOutcome> RegisterAsync(string username, string password, string confirmation, CancellationToken ct = default) => Completion.Task;
    }
    [Fact]
    public void StandardHasherUsesSaltVersionAndRehash()
    {
        var current = new PasswordSecurity();
        const string password = "Test-only password 2026!";
        var first = current.Hash(password);
        var second = current.Hash(password);
        Assert.NotEqual(password, first);
        Assert.NotEqual(first, second);
        Assert.Equal(1, Convert.FromBase64String(first)[0]);
        Assert.Equal(PasswordCheck.Success, current.Verify(first, password));
        Assert.Equal(PasswordCheck.Failed, current.Verify(first, "incorrect password"));
        Assert.Equal(PasswordCheck.Failed, current.Verify("not-a-hash", password));
        var old = new PasswordSecurity(100_000).Hash(password);
        Assert.Equal(PasswordCheck.RehashNeeded, current.Verify(old, password));
        var input = new PasswordInput(password);
        Assert.DoesNotContain(password, input.ToString());
        input.Clear();
        Assert.Empty(input.Password);
    }
    [Theory]
    [InlineData("ab", false)]
    [InlineData("normal-user", true)]
    [InlineData("name with spaces", false)]
    [InlineData(" user", false)]
    [InlineData("имя-пользователя", true)]
    public void UsernameValidation(string username, bool valid) => Assert.Equal(valid, IdentityValidation.ValidUsername(username));
}
