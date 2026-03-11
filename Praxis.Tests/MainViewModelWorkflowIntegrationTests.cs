using Praxis.Core.Models;
using Praxis.Services;
using Praxis.ViewModels;

namespace Praxis.Tests;

public class MainViewModelWorkflowIntegrationTests
{
    [Fact]
    public async Task Create_Edit_Execute_ThenExternalSync_UpdatesStateAndNotifies()
    {
        var repository = new InMemoryAppRepository();
        var executor = new RecordingCommandExecutor((true, "ok"));
        var clipboard = new RecordingClipboardService();
        var theme = new RecordingThemeService();
        var syncNotifier = new TestStateSyncNotifier();
        var viewModel = new MainViewModel(repository, executor, clipboard, theme, syncNotifier);

        await viewModel.InitializeAsync();

        viewModel.CreateNewCommand.Execute(null);
        Assert.True(viewModel.IsEditorOpen);
        viewModel.Editor.Command = "build";
        viewModel.Editor.ButtonText = "Build";
        viewModel.Editor.Tool = "echo";
        viewModel.Editor.Arguments = "v1";
        viewModel.Editor.UseInvertedThemeColors = true;

        await viewModel.SaveEditorCommand.ExecuteAsync(null);

        var created = Assert.Single(await repository.GetButtonsAsync());
        Assert.Equal("Build", created.ButtonText);
        Assert.Equal("v1", created.Arguments);
        Assert.True(created.UseInvertedThemeColors);
        Assert.Equal(1, syncNotifier.NotifyCount);

        var createdVm = Assert.Single(viewModel.VisibleButtons);
        viewModel.OpenEditorCommand.Execute(createdVm);
        Assert.True(viewModel.IsEditorOpen);
        viewModel.Editor.ButtonText = "Build Updated";
        viewModel.Editor.Arguments = "v2";
        viewModel.Editor.UseInvertedThemeColors = false;

        await viewModel.SaveEditorCommand.ExecuteAsync(null);

        var edited = Assert.Single(await repository.GetButtonsAsync());
        Assert.Equal("Build Updated", edited.ButtonText);
        Assert.Equal("v2", edited.Arguments);
        Assert.False(edited.UseInvertedThemeColors);
        Assert.Equal(2, syncNotifier.NotifyCount);

        var editedVm = Assert.Single(viewModel.VisibleButtons);
        await viewModel.ExecuteButtonCommand.ExecuteAsync(editedVm);

        Assert.Single(executor.Calls);
        Assert.Equal("echo", executor.Calls[0].Tool);
        Assert.Equal("v2", executor.Calls[0].Arguments);

        var log = Assert.Single(repository.Logs);
        Assert.Equal(edited.Id, log.ButtonId);
        Assert.Equal("button", log.Source);
        Assert.Equal("echo", log.Tool);
        Assert.Equal("v2", log.Arguments);
        Assert.Equal(3, syncNotifier.NotifyCount);

        repository.UpsertForExternalChange(new LauncherButtonRecord(edited)
        {
            ButtonText = "Build Synced",
        });

        syncNotifier.RaiseExternalChange();

        await WaitUntilAsync(() => viewModel.StatusText == "Synced from another window.");

        Assert.Equal("Build Synced", Assert.Single(viewModel.VisibleButtons).ButtonText);
    }

    [Fact]
    public async Task CommandSuggestions_DoNotAutoSelect_AndDownSelectsFirstItem()
    {
        var repository = new InMemoryAppRepository();
        await repository.UpsertButtonAsync(new LauncherButtonRecord
        {
            Id = Guid.NewGuid(),
            Command = "build",
            ButtonText = "Build",
            Tool = "echo",
            Arguments = "one",
        });
        await repository.UpsertButtonAsync(new LauncherButtonRecord
        {
            Id = Guid.NewGuid(),
            Command = "bundle",
            ButtonText = "Bundle",
            Tool = "echo",
            Arguments = "two",
        });

        var executor = new RecordingCommandExecutor((true, "ok"));
        var clipboard = new RecordingClipboardService();
        var theme = new RecordingThemeService();
        var syncNotifier = new TestStateSyncNotifier();
        var viewModel = new MainViewModel(repository, executor, clipboard, theme, syncNotifier);
        await viewModel.InitializeAsync();

        viewModel.CommandInput = "bu";
        await WaitUntilAsync(() => viewModel.IsCommandSuggestionOpen && viewModel.CommandSuggestions.Count == 2);

        Assert.Equal(-1, viewModel.SelectedCommandSuggestionIndex);
        Assert.Null(viewModel.SelectedCommandSuggestion);
        Assert.All(viewModel.CommandSuggestions, x => Assert.False(x.IsSelected));

        viewModel.MoveSuggestionDownCommand.Execute(null);

        Assert.Equal(0, viewModel.SelectedCommandSuggestionIndex);
        Assert.NotNull(viewModel.SelectedCommandSuggestion);
        Assert.Same(viewModel.CommandSuggestions[0], viewModel.SelectedCommandSuggestion);
        Assert.True(viewModel.CommandSuggestions[0].IsSelected);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 2000)
    {
        var started = DateTime.UtcNow;
        while (!condition())
        {
            if ((DateTime.UtcNow - started).TotalMilliseconds > timeoutMs)
            {
                throw new TimeoutException("Condition was not met within timeout.");
            }

            await Task.Delay(20);
        }
    }

    private sealed class InMemoryAppRepository : IAppRepository
    {
        private readonly object gate = new();
        private readonly Dictionary<Guid, LauncherButtonRecord> buttons = [];
        private readonly List<LaunchLogEntry> logs = [];
        private readonly List<Guid> dockOrder = [];
        private ThemeMode themeMode = ThemeMode.System;

        public IReadOnlyList<LaunchLogEntry> Logs
        {
            get
            {
                lock (gate)
                {
                    return logs.Select(CloneLog).ToList();
                }
            }
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<LauncherButtonRecord>> GetButtonsAsync(CancellationToken cancellationToken = default)
        {
            lock (gate)
            {
                return Task.FromResult<IReadOnlyList<LauncherButtonRecord>>(buttons.Values
                    .OrderBy(x => x.Y)
                    .ThenBy(x => x.X)
                    .Select(CloneRecord)
                    .ToList());
            }
        }

        public Task<IReadOnlyList<LauncherButtonRecord>> ReloadButtonsAsync(CancellationToken cancellationToken = default)
            => GetButtonsAsync(cancellationToken);

        public Task<LauncherButtonRecord?> GetByIdAsync(Guid id, bool forceReload = false, CancellationToken cancellationToken = default)
        {
            lock (gate)
            {
                return Task.FromResult(buttons.TryGetValue(id, out var record)
                    ? CloneRecord(record)
                    : null);
            }
        }

        public Task<LauncherButtonRecord?> GetByCommandAsync(string command, CancellationToken cancellationToken = default)
        {
            lock (gate)
            {
                var match = buttons.Values.FirstOrDefault(x =>
                    string.Equals(x.Command?.Trim(), command?.Trim(), StringComparison.OrdinalIgnoreCase));

                return Task.FromResult(match is null ? null : CloneRecord(match));
            }
        }

        public Task UpsertButtonAsync(LauncherButtonRecord record, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(record);

            lock (gate)
            {
                var next = CloneRecord(record);
                next.UpdatedAtUtc = DateTime.UtcNow;
                if (!buttons.TryGetValue(record.Id, out var existing))
                {
                    next.CreatedAtUtc = record.CreatedAtUtc == default ? DateTime.UtcNow : record.CreatedAtUtc;
                }
                else
                {
                    next.CreatedAtUtc = existing.CreatedAtUtc;
                }

                buttons[record.Id] = next;
            }

            return Task.CompletedTask;
        }

        public Task DeleteButtonAsync(Guid id, CancellationToken cancellationToken = default)
        {
            lock (gate)
            {
                buttons.Remove(id);
                dockOrder.RemoveAll(x => x == id);
            }

            return Task.CompletedTask;
        }

        public Task AddLogAsync(LaunchLogEntry entry, CancellationToken cancellationToken = default)
        {
            lock (gate)
            {
                logs.Add(CloneLog(entry));
            }

            return Task.CompletedTask;
        }

        public Task PurgeOldLogsAsync(int retentionDays, CancellationToken cancellationToken = default)
        {
            if (retentionDays < 1)
            {
                retentionDays = 1;
            }

            var threshold = DateTime.UtcNow.AddDays(-retentionDays);
            lock (gate)
            {
                logs.RemoveAll(x => x.TimestampUtc < threshold);
            }

            return Task.CompletedTask;
        }

        public Task AddErrorLogAsync(Core.Models.ErrorLogEntry entry, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task PurgeOldErrorLogsAsync(int retentionDays, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SetThemeAsync(ThemeMode mode, CancellationToken cancellationToken = default)
        {
            lock (gate)
            {
                themeMode = mode;
            }

            return Task.CompletedTask;
        }

        public Task<ThemeMode> GetThemeAsync(CancellationToken cancellationToken = default)
        {
            lock (gate)
            {
                return Task.FromResult(themeMode);
            }
        }

        public Task<IReadOnlyList<Guid>> GetDockButtonIdsAsync(CancellationToken cancellationToken = default)
        {
            lock (gate)
            {
                return Task.FromResult<IReadOnlyList<Guid>>(dockOrder.ToList());
            }
        }

        public Task SetDockButtonIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default)
        {
            lock (gate)
            {
                dockOrder.Clear();
                dockOrder.AddRange(ids);
            }

            return Task.CompletedTask;
        }

        public void UpsertForExternalChange(LauncherButtonRecord record)
        {
            lock (gate)
            {
                var next = CloneRecord(record);
                next.UpdatedAtUtc = DateTime.UtcNow;
                if (!buttons.TryGetValue(record.Id, out var existing))
                {
                    next.CreatedAtUtc = record.CreatedAtUtc == default ? DateTime.UtcNow : record.CreatedAtUtc;
                }
                else
                {
                    next.CreatedAtUtc = existing.CreatedAtUtc;
                }

                buttons[record.Id] = next;
            }
        }

        private static LauncherButtonRecord CloneRecord(LauncherButtonRecord source)
            => source.Clone();

        private static LaunchLogEntry CloneLog(LaunchLogEntry source)
            => new()
            {
                Id = source.Id,
                ButtonId = source.ButtonId,
                Source = source.Source,
                Tool = source.Tool,
                Arguments = source.Arguments,
                Succeeded = source.Succeeded,
                Message = source.Message,
                TimestampUtc = source.TimestampUtc,
            };
    }

    private sealed class RecordingCommandExecutor((bool Success, string Message) result) : ICommandExecutor
    {
        public List<(string Tool, string Arguments)> Calls { get; } = [];

        public Task<(bool Success, string Message)> ExecuteAsync(string tool, string arguments, CancellationToken cancellationToken = default)
        {
            Calls.Add((tool, arguments));
            return Task.FromResult(result);
        }
    }

    private sealed class RecordingClipboardService : IClipboardService
    {
        public string CurrentText { get; private set; } = string.Empty;

        public Task<string> GetTextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CurrentText);

        public Task SetTextAsync(string text, CancellationToken cancellationToken = default)
        {
            CurrentText = text;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingThemeService : IThemeService
    {
        public ThemeMode Current { get; private set; } = ThemeMode.System;

        public void Apply(ThemeMode mode)
        {
            Current = mode;
        }
    }

    private sealed class TestStateSyncNotifier : IStateSyncNotifier
    {
        public int NotifyCount { get; private set; }

        public event EventHandler<StateSyncChangedEventArgs>? ButtonsChanged;

        public Task NotifyButtonsChangedAsync(CancellationToken cancellationToken = default)
        {
            NotifyCount++;
            return Task.CompletedTask;
        }

        public void RaiseExternalChange()
        {
            ButtonsChanged?.Invoke(this, new StateSyncChangedEventArgs
            {
                SourceInstanceId = Guid.NewGuid().ToString("N"),
                TimestampUtc = DateTime.UtcNow,
            });
        }

        public void Dispose()
        {
        }
    }
}
