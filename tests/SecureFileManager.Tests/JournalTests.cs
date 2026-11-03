using SecureFileManager.Core.Entities;
using SecureFileManager.Core.Interfaces;
using SecureFileManager.Core.Journal;
using SecureFileManager.Core.Results;
using SecureFileManager.Wpf.ViewModels;

namespace SecureFileManager.Tests;
public sealed class JournalTests
{
    private sealed class Clock : IClock { public DateTime UtcNow { get; set; } = DateTime.UtcNow; }
    private sealed class Probe(Func<Task<bool>> action) : IAuditProbe { public Task<bool> CheckAsync(CancellationToken ct = default) => action(); }
    [Fact]
    public async Task HealthExpiresLatchesFailuresAndDoesNotLetOldProbeHideWriteFailure()
    {
        var clock = new Clock(); var health = new AuditHealth(clock); Assert.Equal(AuditState.Unknown, health.Snapshot.State);
        health.Succeeded(); Assert.Equal(AuditState.Active, health.Snapshot.State);
        clock.UtcNow += TimeSpan.FromMinutes(3); Assert.Equal(AuditState.Unknown, health.Snapshot.State);
        var pending = new TaskCompletionSource<bool>(); var checking = health.ProbeAsync(new Probe(() => pending.Task), default);
        Assert.Equal(AuditState.Checking, health.Snapshot.State); health.Failed(); pending.SetResult(true); await checking;
        Assert.Equal(AuditState.Unavailable, health.Snapshot.State);
        health.Succeeded(); Assert.Equal(AuditState.Unavailable, health.Snapshot.State);
        await health.ProbeAsync(new Probe(() => Task.FromResult(true)), default); Assert.Equal(AuditState.Active, health.Snapshot.State);
        await health.ProbeAsync(new Probe(() => Task.FromException<bool>(new Exception("secret exception"))), default);
        using var model = new AuditStatusViewModel(health);
        Assert.Equal("Журнал: недоступен", model.Text); Assert.DoesNotContain("secret", model.Evidence);
    }
    [Fact]
    public void JournalProjectionNeverDisplaysRawExceptionDetailsOrAbsolutePaths()
    {
        var source = new Operation { Details = "PasswordHash=SECRET; connection=SECRET; document=SECRET", TargetPathSnapshot = @"C:\private\SECRET",
            OperationType = OperationType.OPERATION_FAILED, Result = OperationResult.Failed };
        var item = AuditListItem.From(source); Assert.DoesNotContain("SECRET", item.Description); Assert.Equal("—", item.Target);
        Assert.Equal("Failed", item.ResultCode);
        source.TargetPathSnapshot = "owned/name.txt"; Assert.Equal("owned/name.txt", AuditListItem.From(source).Target);
    }
    private sealed class DelayedJournal : IJournalService
    {
        public TaskCompletionSource<RepositoryResult<JournalPage>> Pending { get; } = new();
        public Task CheckHealthAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task<RepositoryResult<JournalPage>> GetAsync(JournalQuery query, CancellationToken ct = default) => Pending.Task;
    }
    [Fact]
    public async Task JournalVmDoesNotDisplayLatePreviousSessionResultAndClearsFilters()
    {
        var service = new DelayedJournal(); var vm = new JournalViewModel(service); vm.UpdateSession(new(Guid.NewGuid(), "first"));
        vm.SearchText = "private"; var load = vm.LoadAsync(); Assert.True(vm.IsLoading);
        vm.UpdateSession(new(Guid.NewGuid(), "second"));
        service.Pending.SetResult(RepositoryResult<JournalPage>.Ok(new([new Operation { TargetPathSnapshot = "private.txt" }], false)));
        await load; Assert.Empty(vm.Items); Assert.Empty(vm.SearchText); Assert.False(vm.IsLoading);
    }
    private sealed class FailedJournal : IJournalService
    {
        public Task CheckHealthAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task<RepositoryResult<JournalPage>> GetAsync(JournalQuery query, CancellationToken ct = default) =>
            Task.FromResult(RepositoryResult<JournalPage>.Fail(RepositoryStatus.InfrastructureError));
    }
    [Fact]
    public async Task JournalVmDisplaysDatabaseFailureAndDoesNotKeepOldRows()
    {
        var vm = new JournalViewModel(new FailedJournal()); vm.UpdateSession(new(Guid.NewGuid(), "user"));
        vm.Items.Add(new("", "", "", "", "")); await vm.LoadAsync(); Assert.Empty(vm.Items);
        Assert.Contains("подключение", vm.ErrorMessage); Assert.False(vm.IsLoading);
    }
    [Fact]
    public void BillionLaughsRejectedBeforeExpansion()
    {
        var result = Core.Files.DocumentValidator.Validate(Core.Files.DocumentFormat.Xml,
            "<!DOCTYPE lolz [<!ENTITY lol 'lol'><!ENTITY lol1 '&lol;&lol;&lol;&lol;'><!ENTITY lol2 '&lol1;&lol1;&lol1;&lol1;'>]><r>&lol2;</r>");
        Assert.True(result.Prohibited); Assert.Equal("XML_DTD", result.Code);
    }
}
