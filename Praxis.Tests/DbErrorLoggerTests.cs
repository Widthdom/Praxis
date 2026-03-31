using Praxis.Core.Models;
using Praxis.Services;

namespace Praxis.Tests;

public class DbErrorLoggerTests
{
    [Fact]
    public void Log_DoesNotThrow()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        var ex = Record.Exception(() => logger.Log(new InvalidOperationException("test"), "context"));
        Assert.Null(ex);
    }

    [Fact]
    public void LogInfo_DoesNotThrow()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        var ex = Record.Exception(() => logger.LogInfo("info message", "context"));
        Assert.Null(ex);
    }

    [Fact]
    public void LogWarning_DoesNotThrow()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        var ex = Record.Exception(() => logger.LogWarning("warn message", "context"));
        Assert.Null(ex);
    }

    [Fact]
    public async Task FlushAsync_WritesPendingEntriesToRepository()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        logger.LogInfo("flush test", "context");

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        Assert.Contains(repo.ErrorLogs, e => e.Message == "flush test" && e.Level == "Info");
    }

    [Fact]
    public async Task FlushAsync_WritesErrorEntriesToRepository()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        logger.Log(new InvalidOperationException("error test"), "ctx");

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        Assert.Contains(repo.ErrorLogs, e => e.Level == "Error" && e.Message.Contains("error test"));
    }

    [Fact]
    public async Task FlushAsync_WritesWarningEntriesToRepository()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        logger.LogWarning("warn test", "ctx");

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        Assert.Contains(repo.ErrorLogs, e => e.Level == "Warning" && e.Message == "warn test");
    }

    [Fact]
    public async Task Log_CapturesExceptionTypeChain()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        var inner = new NullReferenceException("inner");
        var outer = new InvalidOperationException("outer", inner);
        logger.Log(outer, "ctx");

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Contains("InvalidOperationException", entry.ExceptionType);
        Assert.Contains("NullReferenceException", entry.ExceptionType);
        Assert.Contains("->", entry.ExceptionType);
    }

    [Fact]
    public async Task Log_CapturesFullMessageChain()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        var inner = new NullReferenceException("inner msg");
        var outer = new InvalidOperationException("outer msg", inner);
        logger.Log(outer, "ctx");

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Contains("outer msg", entry.Message);
        Assert.Contains("inner msg", entry.Message);
    }

    [Fact]
    public async Task Log_CapturesFullStackTrace()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        Exception caught;
        try { throw new InvalidOperationException("stack test"); }
        catch (Exception ex) { caught = ex; }

        logger.Log(caught, "ctx");
        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Contains("InvalidOperationException", entry.StackTrace);
        Assert.Contains("stack test", entry.StackTrace);
    }

    [Fact]
    public async Task Log_AggregateException_CapturesAllInnerTypes()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        var agg = new AggregateException("agg",
            new InvalidOperationException("first"),
            new ArgumentException("second"));
        logger.Log(agg, "ctx");

        await logger.FlushAsync(TimeSpan.FromSeconds(5));

        var entry = Assert.Single(repo.ErrorLogs);
        Assert.Contains("AggregateException", entry.ExceptionType);
        Assert.Contains("InvalidOperationException", entry.ExceptionType);
        Assert.Contains("ArgumentException", entry.ExceptionType);
    }

    [Fact]
    public async Task FlushAsync_RespectsTimeout()
    {
        var repo = new FakeAppRepository();
        var logger = new DbErrorLogger(repo);

        // Should complete quickly even with a very short timeout when queue is empty.
        await logger.FlushAsync(TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Log_DoesNotThrow_WhenRepositoryFails()
    {
        var repo = new FailingAppRepository();
        var logger = new DbErrorLogger(repo);

        logger.Log(new Exception("repo fail test"), "ctx");

        // Should not throw even though the repository fails.
        var ex = await Record.ExceptionAsync(() => logger.FlushAsync(TimeSpan.FromSeconds(2)));
        Assert.Null(ex);
    }

    private sealed class FakeAppRepository : IAppRepository
    {
        public List<ErrorLogEntry> ErrorLogs { get; } = [];
        public int PurgeCallCount { get; private set; }

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<LauncherButtonRecord>> GetButtonsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>([]);
        public Task<IReadOnlyList<LauncherButtonRecord>> ReloadButtonsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>([]);
        public Task<LauncherButtonRecord?> GetByIdAsync(Guid id, bool forceReload = false, CancellationToken cancellationToken = default) => Task.FromResult<LauncherButtonRecord?>(null);
        public Task<LauncherButtonRecord?> GetByCommandAsync(string command, CancellationToken cancellationToken = default) => Task.FromResult<LauncherButtonRecord?>(null);
        public Task UpsertButtonAsync(LauncherButtonRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteButtonAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddLogAsync(LaunchLogEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PurgeOldLogsAsync(int retentionDays, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task AddErrorLogAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default)
        {
            ErrorLogs.Add(entry);
            return Task.CompletedTask;
        }

        public Task PurgeOldErrorLogsAsync(int retentionDays, CancellationToken cancellationToken = default)
        {
            PurgeCallCount++;
            return Task.CompletedTask;
        }

        public Task SetThemeAsync(ThemeMode themeMode, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ThemeMode> GetThemeAsync(CancellationToken cancellationToken = default) => Task.FromResult(ThemeMode.System);
        public Task<IReadOnlyList<Guid>> GetDockButtonIdsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task SetDockButtonIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FailingAppRepository : IAppRepository
    {
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<LauncherButtonRecord>> GetButtonsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>([]);
        public Task<IReadOnlyList<LauncherButtonRecord>> ReloadButtonsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>([]);
        public Task<LauncherButtonRecord?> GetByIdAsync(Guid id, bool forceReload = false, CancellationToken cancellationToken = default) => Task.FromResult<LauncherButtonRecord?>(null);
        public Task<LauncherButtonRecord?> GetByCommandAsync(string command, CancellationToken cancellationToken = default) => Task.FromResult<LauncherButtonRecord?>(null);
        public Task UpsertButtonAsync(LauncherButtonRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteButtonAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddLogAsync(LaunchLogEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PurgeOldLogsAsync(int retentionDays, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddErrorLogAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default) => throw new Exception("Simulated DB failure");
        public Task PurgeOldErrorLogsAsync(int retentionDays, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetThemeAsync(ThemeMode themeMode, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ThemeMode> GetThemeAsync(CancellationToken cancellationToken = default) => Task.FromResult(ThemeMode.System);
        public Task<IReadOnlyList<Guid>> GetDockButtonIdsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task SetDockButtonIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
