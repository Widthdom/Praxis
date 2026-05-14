using Praxis.Core.Logic;
using Praxis.Core.Models;

namespace Praxis.Core.Services;

public sealed class InMemoryLauncherButtonRepository : ILauncherButtonRepository
{
    private readonly List<LauncherButtonRecord> buttons = [];
    private readonly List<LaunchLogEntry> launchLogs = [];
    private List<Guid> dockButtonIds = [];

    public Task InitializeAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<LauncherButtonRecord>> GetButtonsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>(
            LauncherButtonOrderPolicy.ToSortedList(buttons.Select(static button => button.Clone())));

    public Task<IReadOnlyList<LauncherButtonRecord>> ReloadButtonsAsync(CancellationToken cancellationToken = default)
        => GetButtonsAsync(cancellationToken);

    public Task<LauncherButtonRecord?> GetByIdAsync(
        Guid id,
        bool forceReload = false,
        CancellationToken cancellationToken = default)
    {
        var record = buttons.FirstOrDefault(button => button.Id == id);
        return Task.FromResult(record?.Clone());
    }

    public Task<LauncherButtonRecord?> GetByCommandAsync(
        string command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return Task.FromResult<LauncherButtonRecord?>(null);
        }

        var record = buttons.FirstOrDefault(button =>
            string.Equals(button.Command.Trim(), command.Trim(), StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(record?.Clone());
    }

    public Task UpsertButtonAsync(LauncherButtonRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var stored = record.Clone();
        stored.UpdatedAtUtc = DateTime.UtcNow;
        var index = buttons.FindIndex(button => button.Id == stored.Id);
        if (index >= 0)
        {
            buttons[index] = stored;
        }
        else
        {
            buttons.Add(stored);
        }

        return Task.CompletedTask;
    }

    public Task DeleteButtonAsync(Guid id, CancellationToken cancellationToken = default)
    {
        buttons.RemoveAll(button => button.Id == id);
        dockButtonIds.RemoveAll(buttonId => buttonId == id);
        return Task.CompletedTask;
    }

    public Task AddLogAsync(LaunchLogEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        launchLogs.Add(new LaunchLogEntry
        {
            Id = entry.Id,
            ButtonId = entry.ButtonId,
            Source = entry.Source,
            Tool = entry.Tool,
            Arguments = entry.Arguments,
            Succeeded = entry.Succeeded,
            Message = entry.Message,
            TimestampUtc = entry.TimestampUtc,
        });
        return Task.CompletedTask;
    }

    public Task PurgeOldLogsAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        var safeRetentionDays = Math.Max(1, retentionDays);
        var threshold = DateTime.UtcNow.AddDays(-safeRetentionDays);
        launchLogs.RemoveAll(log => log.TimestampUtc < threshold);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Guid>> GetDockButtonIdsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Guid>>(dockButtonIds.ToList());

    public Task SetDockButtonIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default)
    {
        dockButtonIds = DockOrderValueCodec.Parse(DockOrderValueCodec.Serialize(ids)).ToList();
        return Task.CompletedTask;
    }
}
