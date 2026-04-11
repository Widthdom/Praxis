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
        var viewModel = new MainViewModel(repository, executor, clipboard, theme, syncNotifier, new NullErrorLogger());

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
        var viewModel = new MainViewModel(repository, executor, clipboard, theme, syncNotifier, new NullErrorLogger());
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

    [Fact]
    public async Task SetThemeCommand_InvalidNumericInput_FallsBackToSystem()
    {
        var repository = new InMemoryAppRepository();
        var executor = new RecordingCommandExecutor((true, "ok"));
        var clipboard = new RecordingClipboardService();
        var theme = new RecordingThemeService();
        var syncNotifier = new TestStateSyncNotifier();
        var viewModel = new MainViewModel(repository, executor, clipboard, theme, syncNotifier, new NullErrorLogger());
        await viewModel.InitializeAsync();

        await viewModel.SetThemeCommand.ExecuteAsync("999");

        Assert.Equal(ThemeMode.System, viewModel.SelectedTheme);
        Assert.Equal(ThemeMode.System, theme.Current);
        Assert.Equal(ThemeMode.System, await repository.GetThemeAsync());
        Assert.Equal(1, syncNotifier.NotifyCount);
    }

    [Fact]
    public async Task ExternalSync_WithEmptyDockOrder_ClearsDockButtons()
    {
        var repository = new InMemoryAppRepository();
        var record = new LauncherButtonRecord
        {
            Id = Guid.NewGuid(),
            Command = "build",
            ButtonText = "Build",
            Tool = "echo",
            Arguments = "one",
        };
        await repository.UpsertButtonAsync(record);

        var executor = new RecordingCommandExecutor((true, "ok"));
        var clipboard = new RecordingClipboardService();
        var theme = new RecordingThemeService();
        var syncNotifier = new TestStateSyncNotifier();
        var viewModel = new MainViewModel(repository, executor, clipboard, theme, syncNotifier, new RecordingErrorLogger());
        await viewModel.InitializeAsync();

        await viewModel.ExecuteButtonCommand.ExecuteAsync(Assert.Single(viewModel.VisibleButtons));
        Assert.Single(viewModel.DockButtons);

        repository.SetDockOrderForExternalChange([]);
        syncNotifier.RaiseExternalChange();

        await WaitUntilAsync(() => viewModel.DockButtons.Count == 0);
        Assert.Empty(viewModel.DockButtons);
    }

    [Fact]
    public async Task ExternalSync_WhenReloadThrows_LogsWarning()
    {
        var repository = new InMemoryAppRepository();
        var executor = new RecordingCommandExecutor((true, "ok"));
        var clipboard = new RecordingClipboardService();
        var theme = new RecordingThemeService();
        var syncNotifier = new TestStateSyncNotifier();
        var logger = new RecordingErrorLogger();
        var viewModel = new MainViewModel(repository, executor, clipboard, theme, syncNotifier, logger);
        await viewModel.InitializeAsync();
        repository.ThrowOnReloadButtons = new InvalidOperationException("reload boom");

        syncNotifier.RaiseExternalChange();

        await WaitUntilAsync(() => logger.Warnings.Any(x => x.Context == "ReloadFromExternalChangeAsync"));
        Assert.Contains(logger.Warnings, x => x.Message.Contains("reload boom", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExternalThemeSync_WhenThemeReadThrows_LogsWarning()
    {
        var repository = new InMemoryAppRepository();
        var executor = new RecordingCommandExecutor((true, "ok"));
        var clipboard = new RecordingClipboardService();
        var theme = new RecordingThemeService();
        var syncNotifier = new TestStateSyncNotifier();
        var logger = new RecordingErrorLogger();
        var viewModel = new MainViewModel(repository, executor, clipboard, theme, syncNotifier, logger);
        await viewModel.InitializeAsync();

        viewModel.CreateNewCommand.Execute(null);
        Assert.True(viewModel.IsEditorOpen);
        repository.ThrowOnGetTheme = new InvalidOperationException("theme boom");

        syncNotifier.RaiseExternalChange();

        await WaitUntilAsync(() => logger.Warnings.Any(x => x.Context == "SyncThemeFromExternalChangeAsync"));
        Assert.Contains(logger.Warnings, x => x.Message.Contains("theme boom", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExternalSync_WhileEditorOpen_LogsDeferredAndAppliesAfterClose()
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

        var executor = new RecordingCommandExecutor((true, "ok"));
        var clipboard = new RecordingClipboardService();
        var theme = new RecordingThemeService();
        var syncNotifier = new TestStateSyncNotifier();
        var logger = new RecordingErrorLogger();
        var viewModel = new MainViewModel(repository, executor, clipboard, theme, syncNotifier, logger);
        await viewModel.InitializeAsync();

        viewModel.CreateNewCommand.Execute(null);
        Assert.True(viewModel.IsEditorOpen);

        repository.UpsertForExternalChange(new LauncherButtonRecord((await repository.GetButtonsAsync()).Single())
        {
            ButtonText = "Build Synced",
        });

        syncNotifier.RaiseExternalChange();

        await WaitUntilAsync(() => logger.Infos.Any(x => x.Context == "StateSyncNotifierOnButtonsChanged" && x.Message.Contains("deferred", StringComparison.OrdinalIgnoreCase)));

        viewModel.CancelEditorCommand.Execute(null);

        await WaitUntilAsync(() => viewModel.StatusText == "Synced from another window.");
        Assert.Equal("Build Synced", Assert.Single(viewModel.VisibleButtons).ButtonText);
        Assert.Contains(logger.Infos, x => x.Context == "OnIsEditorOpenChanged" && x.Message.Contains("deferred sync", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteButtonCommand_LogsExecutionRequestAndCompletion()
    {
        var repository = new InMemoryAppRepository();
        await repository.UpsertButtonAsync(new LauncherButtonRecord
        {
            Id = Guid.NewGuid(),
            Command = "build",
            ButtonText = "Build",
            Tool = "echo",
            Arguments = "one",
            ClipText = "copied",
        });

        var executor = new RecordingCommandExecutor((true, "ok"));
        var clipboard = new RecordingClipboardService();
        var theme = new RecordingThemeService();
        var syncNotifier = new TestStateSyncNotifier();
        var logger = new RecordingErrorLogger();
        var viewModel = new MainViewModel(repository, executor, clipboard, theme, syncNotifier, logger);
        await viewModel.InitializeAsync();

        await viewModel.ExecuteButtonCommand.ExecuteAsync(Assert.Single(viewModel.VisibleButtons));

        Assert.Contains(logger.Infos, x => x.Context == "ExecuteRecordAsync" && x.Message.Contains("Execution requested (button)", StringComparison.Ordinal));
        Assert.Contains(logger.Infos, x => x.Context == "ExecuteRecordAsync" && x.Message.Contains("Clipboard updated", StringComparison.Ordinal));
        Assert.Contains(logger.Infos, x => x.Context == "ExecuteRecordAsync" && x.Message.Contains("Executed (button)", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SaveEditor_WhenConflictDialogThrows_LogsWarningAndCancels()
    {
        var repository = new InMemoryAppRepository();
        var original = new LauncherButtonRecord
        {
            Id = Guid.NewGuid(),
            Command = "build",
            ButtonText = "Build",
            Tool = "echo",
            Arguments = "one",
            Note = "original",
        };
        await repository.UpsertButtonAsync(original);

        var executor = new RecordingCommandExecutor((true, "ok"));
        var clipboard = new RecordingClipboardService();
        var theme = new RecordingThemeService();
        var syncNotifier = new TestStateSyncNotifier();
        var logger = new RecordingErrorLogger();
        var viewModel = new MainViewModel(repository, executor, clipboard, theme, syncNotifier, logger);
        await viewModel.InitializeAsync();

        viewModel.OpenEditorCommand.Execute(Assert.Single(viewModel.VisibleButtons));
        viewModel.Editor.Note = "edited locally";

        repository.UpsertForExternalChange(new LauncherButtonRecord(original)
        {
            Note = "changed elsewhere",
        });

        viewModel.ResolveEditorConflictAsync = _ => throw new InvalidOperationException("dialog boom");

        await viewModel.SaveEditorCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsEditorOpen);
        Assert.Equal("Save canceled due to conflict.", viewModel.StatusText);
        Assert.Contains(logger.Warnings, x => x.Context == "ResolveConflictAsync" && x.Message.Contains("dialog boom", StringComparison.Ordinal));
    }

    [Fact]
    public async Task OpenCreateEditorAtAsync_WhenClipboardReadFails_UsesEmptyArgsAndLogsWarning()
    {
        var repository = new InMemoryAppRepository();
        var executor = new RecordingCommandExecutor((true, "ok"));
        var clipboard = new RecordingClipboardService
        {
            ThrowOnGetText = new InvalidOperationException("clipboard read boom"),
        };
        var theme = new RecordingThemeService();
        var syncNotifier = new TestStateSyncNotifier();
        var logger = new RecordingErrorLogger();
        var viewModel = new MainViewModel(repository, executor, clipboard, theme, syncNotifier, logger);
        await viewModel.InitializeAsync();

        await viewModel.OpenCreateEditorAtAsync(20, 20, useClipboardForArguments: true);

        Assert.True(viewModel.IsEditorOpen);
        Assert.Equal(string.Empty, viewModel.Editor.Arguments);
        Assert.Contains(logger.Warnings, x => x.Context == "OpenCreateEditorAtAsync" && x.Message.Contains("clipboard read boom", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CopyField_WhenClipboardWriteFails_LogsWarningAndSetsFailureStatus()
    {
        var repository = new InMemoryAppRepository();
        var executor = new RecordingCommandExecutor((true, "ok"));
        var clipboard = new RecordingClipboardService
        {
            ThrowOnSetText = new InvalidOperationException("clipboard write boom"),
        };
        var theme = new RecordingThemeService();
        var syncNotifier = new TestStateSyncNotifier();
        var logger = new RecordingErrorLogger();
        var viewModel = new MainViewModel(repository, executor, clipboard, theme, syncNotifier, logger);
        await viewModel.InitializeAsync();

        await viewModel.CopyFieldCommand.ExecuteAsync("abc");

        Assert.Equal("Clipboard copy failed.", viewModel.StatusText);
        Assert.Contains(logger.Warnings, x => x.Context == "CopyFieldAsync" && x.Message.Contains("clipboard write boom", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteButton_WhenClipboardWriteFails_StillLogsLaunchAndSucceeds()
    {
        var repository = new InMemoryAppRepository();
        await repository.UpsertButtonAsync(new LauncherButtonRecord
        {
            Id = Guid.NewGuid(),
            Command = "build",
            ButtonText = "Build",
            Tool = "echo",
            Arguments = "one",
            ClipText = "copied",
        });

        var executor = new RecordingCommandExecutor((true, "ok"));
        var clipboard = new RecordingClipboardService
        {
            ThrowOnSetText = new InvalidOperationException("clipboard write boom"),
        };
        var theme = new RecordingThemeService();
        var syncNotifier = new TestStateSyncNotifier();
        var logger = new RecordingErrorLogger();
        var viewModel = new MainViewModel(repository, executor, clipboard, theme, syncNotifier, logger);
        await viewModel.InitializeAsync();

        await viewModel.ExecuteButtonCommand.ExecuteAsync(Assert.Single(viewModel.VisibleButtons));

        Assert.Single(executor.Calls);
        Assert.Single(repository.Logs);
        Assert.Equal("Executed.", viewModel.StatusText);
        Assert.Contains(logger.Warnings, x => x.Context == "ExecuteRecordAsync" && x.Message.Contains("clipboard write boom", StringComparison.Ordinal));
        Assert.Contains(logger.Infos, x => x.Context == "ExecuteRecordAsync" && x.Message.Contains("Executed (button)", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SaveEditor_WhenSyncNotifyFails_StillPersistsAndLogsWarning()
    {
        var repository = new InMemoryAppRepository();
        var executor = new RecordingCommandExecutor((true, "ok"));
        var clipboard = new RecordingClipboardService();
        var theme = new RecordingThemeService();
        var syncNotifier = new TestStateSyncNotifier
        {
            ThrowOnNotify = new InvalidOperationException("sync boom"),
        };
        var logger = new RecordingErrorLogger();
        var viewModel = new MainViewModel(repository, executor, clipboard, theme, syncNotifier, logger);
        await viewModel.InitializeAsync();

        viewModel.CreateNewCommand.Execute(null);
        viewModel.Editor.Command = "build";
        viewModel.Editor.ButtonText = "Build";
        viewModel.Editor.Tool = "echo";
        viewModel.Editor.Arguments = "one";

        await viewModel.SaveEditorCommand.ExecuteAsync(null);

        Assert.Single(await repository.GetButtonsAsync());
        Assert.False(viewModel.IsEditorOpen);
        Assert.Equal("Saved.", viewModel.StatusText);
        Assert.Contains(logger.Warnings, x => x.Context == "SaveEditorAsync" && x.Message.Contains("sync boom", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DeleteButton_WhenSyncNotifyFails_StillDeletesAndLogsWarning()
    {
        var repository = new InMemoryAppRepository();
        var record = new LauncherButtonRecord
        {
            Id = Guid.NewGuid(),
            Command = "build",
            ButtonText = "Build",
            Tool = "echo",
            Arguments = "one",
        };
        await repository.UpsertButtonAsync(record);

        var executor = new RecordingCommandExecutor((true, "ok"));
        var clipboard = new RecordingClipboardService();
        var theme = new RecordingThemeService();
        var syncNotifier = new TestStateSyncNotifier
        {
            ThrowOnNotify = new InvalidOperationException("sync boom"),
        };
        var logger = new RecordingErrorLogger();
        var viewModel = new MainViewModel(repository, executor, clipboard, theme, syncNotifier, logger);
        await viewModel.InitializeAsync();

        await viewModel.DeleteButtonCommand.ExecuteAsync(Assert.Single(viewModel.VisibleButtons));

        Assert.Empty(await repository.GetButtonsAsync());
        Assert.Equal("Button deleted.", viewModel.StatusText);
        Assert.Contains(logger.Warnings, x => x.Context == "DeleteButtonsAsync" && x.Message.Contains("sync boom", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SetTheme_WhenSyncNotifyFails_StillAppliesThemeAndLogsWarning()
    {
        var repository = new InMemoryAppRepository();
        var executor = new RecordingCommandExecutor((true, "ok"));
        var clipboard = new RecordingClipboardService();
        var theme = new RecordingThemeService();
        var syncNotifier = new TestStateSyncNotifier
        {
            ThrowOnNotify = new InvalidOperationException("sync boom"),
        };
        var logger = new RecordingErrorLogger();
        var viewModel = new MainViewModel(repository, executor, clipboard, theme, syncNotifier, logger);
        await viewModel.InitializeAsync();

        await viewModel.SetThemeCommand.ExecuteAsync("Dark");

        Assert.Equal(ThemeMode.Dark, viewModel.SelectedTheme);
        Assert.Equal(ThemeMode.Dark, theme.Current);
        Assert.Equal(ThemeMode.Dark, await repository.GetThemeAsync());
        Assert.Contains(logger.Warnings, x => x.Context == "SetThemeAsync" && x.Message.Contains("sync boom", StringComparison.Ordinal));
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
        public Exception? ThrowOnReloadButtons { get; set; }
        public Exception? ThrowOnGetTheme { get; set; }

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
        {
            if (ThrowOnReloadButtons is not null)
            {
                return Task.FromException<IReadOnlyList<LauncherButtonRecord>>(ThrowOnReloadButtons);
            }

            return GetButtonsAsync(cancellationToken);
        }

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
            if (ThrowOnGetTheme is not null)
            {
                return Task.FromException<ThemeMode>(ThrowOnGetTheme);
            }

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

        public void SetDockOrderForExternalChange(IReadOnlyList<Guid> ids)
        {
            lock (gate)
            {
                dockOrder.Clear();
                dockOrder.AddRange(ids);
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
        public Exception? ThrowOnGetText { get; set; }
        public Exception? ThrowOnSetText { get; set; }

        public Task<string> GetTextAsync(CancellationToken cancellationToken = default)
        {
            if (ThrowOnGetText is not null)
            {
                return Task.FromException<string>(ThrowOnGetText);
            }

            return Task.FromResult(CurrentText);
        }

        public Task SetTextAsync(string text, CancellationToken cancellationToken = default)
        {
            if (ThrowOnSetText is not null)
            {
                return Task.FromException(ThrowOnSetText);
            }

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
        public Exception? ThrowOnNotify { get; set; }

        public event EventHandler<StateSyncChangedEventArgs>? ButtonsChanged;

        public Task NotifyButtonsChangedAsync(CancellationToken cancellationToken = default)
        {
            if (ThrowOnNotify is not null)
            {
                return Task.FromException(ThrowOnNotify);
            }

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

    private sealed class NullErrorLogger : IErrorLogger
    {
        public void Log(Exception exception, string context) { }
        public void LogWarning(string message, string context) { }
        public void LogInfo(string message, string context) { }
        public Task FlushAsync(TimeSpan timeout) => Task.CompletedTask;
    }

    private sealed class RecordingErrorLogger : IErrorLogger
    {
        public List<(string Message, string Context)> Infos { get; } = [];
        public List<(string Message, string Context)> Warnings { get; } = [];

        public void Log(Exception exception, string context)
        {
        }

        public void LogWarning(string message, string context)
        {
            Warnings.Add((message, context));
        }

        public void LogInfo(string message, string context)
        {
            Infos.Add((message, context));
        }

        public Task FlushAsync(TimeSpan timeout) => Task.CompletedTask;
    }
}
