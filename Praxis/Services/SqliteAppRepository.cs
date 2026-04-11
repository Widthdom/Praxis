using Praxis.Core.Logic;
using Praxis.Core.Models;
using Praxis.Models;
using SQLite;

namespace Praxis.Services;

public sealed class SqliteAppRepository : IAppRepository
{
    private const string ThemeKey = "theme";
    private const string DockOrderKey = "dock_order";

    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly string dbPath;
    private SQLiteAsyncConnection? connection;
    private List<LauncherButtonRecord> cache = [];
    private readonly Dictionary<string, LauncherButtonRecord> commandCache = new(StringComparer.OrdinalIgnoreCase);

    public SqliteAppRepository()
    {
        AppStoragePaths.PrepareStorage();
        dbPath = AppStoragePaths.DatabasePath;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await InitializeCoreAsync();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<IReadOnlyList<LauncherButtonRecord>> GetButtonsAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await InitializeCoreAsync();
            return cache.Select(Clone).ToList();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<IReadOnlyList<LauncherButtonRecord>> ReloadButtonsAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await InitializeCoreAsync();
            var entities = await Connection.Table<LauncherButtonEntity>().ToListAsync();
            cache = LauncherButtonOrderPolicy.ToSortedList(entities.Select(Map));
            RebuildCommandCache();
            return cache.Select(Clone).ToList();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<LauncherButtonRecord?> GetByIdAsync(Guid id, bool forceReload = false, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await InitializeCoreAsync();

            if (forceReload)
            {
                var entity = await Connection.FindAsync<LauncherButtonEntity>(id.ToString());
                if (entity is null)
                {
                    cache.RemoveAll(x => x.Id == id);
                    RebuildCommandCache();
                    return null;
                }

                var mapped = Map(entity);
                var idx = cache.FindIndex(x => x.Id == id);
                if (idx >= 0)
                {
                    cache[idx] = Clone(mapped);
                }
                else
                {
                    cache.Add(Clone(mapped));
                }

                SortCacheByPlacement();
                RebuildCommandCache();
                return Clone(mapped);
            }

            var cached = cache.FirstOrDefault(x => x.Id == id);
            return cached is null ? null : Clone(cached);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<LauncherButtonRecord?> GetByCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return null;
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            await InitializeCoreAsync();
            commandCache.TryGetValue(command, out var match);
            return match is null ? null : Clone(match);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task UpsertButtonAsync(LauncherButtonRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        await gate.WaitAsync(cancellationToken);
        try
        {
            await InitializeCoreAsync();

            var entity = Map(record);
            entity.UpdatedAtUtc = DateTime.UtcNow;

            await Connection.InsertOrReplaceAsync(entity);

            var existing = cache.FindIndex(x => x.Id == record.Id);
            if (existing >= 0)
            {
                cache[existing] = Clone(Map(entity));
            }
            else
            {
                cache.Add(Clone(Map(entity)));
            }

            SortCacheByPlacement();
            RebuildCommandCache();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task DeleteButtonAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await InitializeCoreAsync();
            await Connection.DeleteAsync<LauncherButtonEntity>(id.ToString());
            cache.RemoveAll(x => x.Id == id);
            RebuildCommandCache();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task AddLogAsync(LaunchLogEntry entry, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await InitializeCoreAsync();
            var entity = new LaunchLogEntity
            {
                Id = entry.Id.ToString(),
                ButtonId = entry.ButtonId?.ToString(),
                Source = entry.Source,
                Tool = entry.Tool,
                Arguments = entry.Arguments,
                Succeeded = entry.Succeeded,
                Message = entry.Message,
                TimestampUtc = entry.TimestampUtc,
            };

            await Connection.InsertAsync(entity);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task PurgeOldLogsAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await InitializeCoreAsync();
            if (retentionDays < 1)
            {
                retentionDays = 1;
            }

            var threshold = DateTime.UtcNow.AddDays(-retentionDays);
            await Connection.ExecuteAsync(
                $"DELETE FROM {nameof(LaunchLogEntity)} WHERE {nameof(LaunchLogEntity.TimestampUtc)} < ?",
                threshold);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task AddErrorLogAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await InitializeCoreAsync();
            var entity = new ErrorLogEntity
            {
                Id = entry.Id.ToString(),
                Level = entry.Level,
                Context = entry.Context,
                ExceptionType = entry.ExceptionType,
                Message = entry.Message,
                StackTrace = entry.StackTrace,
                TimestampUtc = entry.TimestampUtc,
            };

            await Connection.InsertAsync(entity);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task PurgeOldErrorLogsAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await InitializeCoreAsync();
            if (retentionDays < 1)
            {
                retentionDays = 1;
            }

            var threshold = DateTime.UtcNow.AddDays(-retentionDays);
            await Connection.ExecuteAsync(
                $"DELETE FROM {nameof(ErrorLogEntity)} WHERE {nameof(ErrorLogEntity.TimestampUtc)} < ?",
                threshold);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task SetThemeAsync(ThemeMode themeMode, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await InitializeCoreAsync();
            var normalized = ThemeModeParser.NormalizeOrDefault(themeMode, ThemeMode.System);
            await Connection.InsertOrReplaceAsync(new AppSettingEntity
            {
                Key = ThemeKey,
                Value = normalized.ToString(),
            });
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<ThemeMode> GetThemeAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await InitializeCoreAsync();
            var setting = await Connection.FindAsync<AppSettingEntity>(ThemeKey);
            if (setting is null)
            {
                return ThemeMode.System;
            }

            return ThemeModeParser.ParseOrDefault(setting.Value, ThemeMode.System);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<IReadOnlyList<Guid>> GetDockButtonIdsAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await InitializeCoreAsync();
            var setting = await Connection.FindAsync<AppSettingEntity>(DockOrderKey);
            if (setting is null || string.IsNullOrWhiteSpace(setting.Value))
            {
                return [];
            }

            return DockOrderValueCodec.Parse(setting.Value);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task SetDockButtonIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await InitializeCoreAsync();
            var value = DockOrderValueCodec.Serialize(ids);
            await Connection.InsertOrReplaceAsync(new AppSettingEntity
            {
                Key = DockOrderKey,
                Value = value,
            });
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task InitializeCoreAsync()
    {
        if (connection is not null)
        {
            return;
        }

        var initializedConnection = new SQLiteAsyncConnection(dbPath);
        await EnsureSchemaAsync(initializedConnection);

        var entities = await initializedConnection.Table<LauncherButtonEntity>().ToListAsync();
        cache = LauncherButtonOrderPolicy.ToSortedList(entities.Select(Map));
        RebuildCommandCache();
        connection = initializedConnection;
    }

    private static async Task EnsureSchemaAsync(SQLiteAsyncConnection connection)
    {
        var currentVersion = await GetSchemaVersionAsync(connection);
        foreach (var targetVersion in DatabaseSchemaVersionPolicy.ResolvePendingUpgradeVersions(currentVersion))
        {
            await ApplySchemaUpgradeAsync(connection, targetVersion);
            await SetSchemaVersionAsync(connection, targetVersion);
        }
    }

    private static Task<int> GetSchemaVersionAsync(SQLiteAsyncConnection connection)
        => connection.ExecuteScalarAsync<int>("PRAGMA user_version;");

    private static Task SetSchemaVersionAsync(SQLiteAsyncConnection connection, int version)
        => connection.ExecuteAsync($"PRAGMA user_version = {version};");

    private static async Task ApplySchemaUpgradeAsync(SQLiteAsyncConnection connection, int targetVersion)
    {
        switch (targetVersion)
        {
            case 1:
                await connection.CreateTableAsync<LauncherButtonEntity>();
                await connection.CreateTableAsync<LaunchLogEntity>();
                await connection.CreateTableAsync<AppSettingEntity>();
                break;
            case 2:
                if (!await ColumnExistsAsync(
                        connection,
                        nameof(LauncherButtonEntity),
                        nameof(LauncherButtonEntity.UseInvertedThemeColors)))
                {
                    await connection.ExecuteAsync(
                        $"ALTER TABLE {nameof(LauncherButtonEntity)} " +
                        $"ADD COLUMN {nameof(LauncherButtonEntity.UseInvertedThemeColors)} INTEGER NOT NULL DEFAULT 0;");
                }

                break;
            case 3:
                await connection.CreateTableAsync<ErrorLogEntity>();
                break;
            case 4:
                if (!await ColumnExistsAsync(
                        connection,
                        nameof(ErrorLogEntity),
                        nameof(ErrorLogEntity.Level)))
                {
                    await connection.ExecuteAsync(
                        $"ALTER TABLE {nameof(ErrorLogEntity)} " +
                        $"ADD COLUMN {nameof(ErrorLogEntity.Level)} TEXT NOT NULL DEFAULT 'Error';");
                }

                break;
            default:
                throw new NotSupportedException($"Unknown schema migration target version: {targetVersion}");
        }
    }

    private static async Task<bool> ColumnExistsAsync(SQLiteAsyncConnection connection, string tableName, string columnName)
    {
        var count = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM pragma_table_info('{tableName}') WHERE name = ?;",
            columnName);
        return count > 0;
    }

    private SQLiteAsyncConnection Connection =>
        connection ?? throw new InvalidOperationException("Repository is not initialized.");

    private static LauncherButtonRecord Map(LauncherButtonEntity x)
        => x.ToRecord();

    private static LauncherButtonEntity Map(LauncherButtonRecord x)
        => new(x);

    private static LauncherButtonRecord Clone(LauncherButtonRecord x)
        => x.Clone();

    private void SortCacheByPlacement()
    {
        cache = LauncherButtonOrderPolicy.ToSortedList(cache);
    }

    private void RebuildCommandCache()
    {
        commandCache.Clear();
        foreach (var button in cache)
        {
            if (string.IsNullOrWhiteSpace(button.Command))
            {
                continue;
            }

            // Keep first occurrence behavior compatible with previous FirstOrDefault.
            if (!commandCache.ContainsKey(button.Command))
            {
                commandCache[button.Command] = button;
            }
        }
    }
}
