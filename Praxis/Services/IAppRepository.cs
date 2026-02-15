using Praxis.Core.Models;

namespace Praxis.Services;

public interface IAppRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LauncherButtonRecord>> GetButtonsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LauncherButtonRecord>> ReloadButtonsAsync(CancellationToken cancellationToken = default);
    Task<LauncherButtonRecord?> GetByIdAsync(Guid id, bool forceReload = false, CancellationToken cancellationToken = default);
    Task<LauncherButtonRecord?> GetByCommandAsync(string command, CancellationToken cancellationToken = default);
    Task UpsertButtonAsync(LauncherButtonRecord record, CancellationToken cancellationToken = default);
    Task DeleteButtonAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddLogAsync(LaunchLogEntry entry, CancellationToken cancellationToken = default);
    Task PurgeOldLogsAsync(int retentionDays, CancellationToken cancellationToken = default);
    Task SetThemeAsync(ThemeMode themeMode, CancellationToken cancellationToken = default);
    Task<ThemeMode> GetThemeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetDockButtonIdsAsync(CancellationToken cancellationToken = default);
    Task SetDockButtonIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default);
}
