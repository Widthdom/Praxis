using Praxis.Core.Logic;
using Praxis.Core.Models;
using Praxis.Core.Services;
using Praxis.Data.Entities;
using Praxis.Data.Storage;
using SQLite;

namespace Praxis.Data.Repositories;

public sealed class SqliteLauncherButtonRepository : ILauncherButtonRepository, IAsyncDisposable
{
    private const string DockOrderKey = "dock_order";

    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly AppStoragePathProvider storagePathProvider;
    private readonly string? explicitDatabasePath;
    private SQLiteAsyncConnection? connection;

    public SqliteLauncherButtonRepository()
        : this(new AppStoragePathProvider())
    {
    }

    public SqliteLauncherButtonRepository(AppStoragePathProvider storagePathProvider)
    {
        this.storagePathProvider = storagePathProvider;
    }

    public SqliteLauncherButtonRepository(string databasePath)
    {
        explicitDatabasePath = databasePath;
        storagePathProvider = new AppStoragePathProvider();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (connection is not null)
            {
                return;
            }

            if (explicitDatabasePath is null)
            {
                storagePathProvider.PrepareStorage();
            }

            var databasePath = explicitDatabasePath ?? storagePathProvider.DatabasePath;
            var directory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var initializedConnection = new SQLiteAsyncConnection(databasePath);
            await EnsureSchemaAsync(initializedConnection);
            connection = initializedConnection;
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
            var entities = await Connection.Table<LauncherButtonEntity>().ToListAsync();
            return LauncherButtonOrderPolicy.ToSortedList(entities.Select(static entity => entity.ToRecord()));
        }
        finally
        {
            gate.Release();
        }
    }

    public Task<IReadOnlyList<LauncherButtonRecord>> ReloadButtonsAsync(CancellationToken cancellationToken = default)
        => GetButtonsAsync(cancellationToken);

    public async Task<LauncherButtonRecord?> GetByIdAsync(
        Guid id,
        bool forceReload = false,
        CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await InitializeCoreAsync();
            var entity = await Connection.FindAsync<LauncherButtonEntity>(id.ToString());
            return entity?.ToRecord();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<LauncherButtonRecord?> GetByCommandAsync(
        string command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return null;
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            await InitializeCoreAsync();
            var normalized = command.Trim();
            var entities = await Connection.Table<LauncherButtonEntity>().ToListAsync();
            return entities
                .Select(static entity => entity.ToRecord())
                .FirstOrDefault(record =>
                    string.Equals(record.Command.Trim(), normalized, StringComparison.OrdinalIgnoreCase));
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
            var copy = record.Clone();
            copy.UpdatedAtUtc = DateTime.UtcNow;
            await Connection.InsertOrReplaceAsync(new LauncherButtonEntity(copy));
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
            var dockIds = await GetDockButtonIdsCoreAsync();
            if (dockIds.Contains(id))
            {
                await SetDockButtonIdsCoreAsync(dockIds.Where(dockId => dockId != id).ToList());
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task AddLogAsync(LaunchLogEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await gate.WaitAsync(cancellationToken);
        try
        {
            await InitializeCoreAsync();
            await Connection.InsertAsync(new LaunchLogEntity
            {
                Id = entry.Id.ToString(),
                ButtonId = entry.ButtonId?.ToString(),
                Source = entry.Source,
                Tool = entry.Tool,
                Arguments = entry.Arguments,
                Succeeded = entry.Succeeded,
                Message = entry.Message,
                TimestampUtc = entry.TimestampUtc,
            });
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
            var safeRetentionDays = Math.Max(1, retentionDays);
            var threshold = DateTime.UtcNow.AddDays(-safeRetentionDays);
            await Connection.ExecuteAsync(
                $"DELETE FROM {nameof(LaunchLogEntity)} WHERE {nameof(LaunchLogEntity.TimestampUtc)} < ?",
                threshold);
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
            return await GetDockButtonIdsCoreAsync();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task SetDockButtonIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        await gate.WaitAsync(cancellationToken);
        try
        {
            await InitializeCoreAsync();
            await SetDockButtonIdsCoreAsync(ids);
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await gate.WaitAsync();
        try
        {
            if (connection is not null)
            {
                await connection.CloseAsync();
                connection = null;
            }
        }
        finally
        {
            gate.Release();
            gate.Dispose();
        }
    }

    private async Task InitializeCoreAsync()
    {
        if (connection is not null)
        {
            return;
        }

        var databasePath = explicitDatabasePath ?? storagePathProvider.DatabasePath;
        if (explicitDatabasePath is null)
        {
            storagePathProvider.PrepareStorage();
        }

        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var initializedConnection = new SQLiteAsyncConnection(databasePath);
        await EnsureSchemaAsync(initializedConnection);
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
                await AddColumnIfMissingAsync(
                    connection,
                    nameof(LauncherButtonEntity),
                    nameof(LauncherButtonEntity.UseInvertedThemeColors),
                    "INTEGER NOT NULL DEFAULT 0");
                break;
            case 3:
                await connection.CreateTableAsync<ErrorLogEntity>();
                break;
            case 4:
                await AddColumnIfMissingAsync(
                    connection,
                    nameof(ErrorLogEntity),
                    nameof(ErrorLogEntity.Level),
                    "TEXT NOT NULL DEFAULT 'Error'");
                break;
            case 5:
                await AddColumnIfMissingAsync(
                    connection,
                    nameof(LauncherButtonEntity),
                    nameof(LauncherButtonEntity.ColorKey),
                    "TEXT NOT NULL DEFAULT 'Default'");
                await AddColumnIfMissingAsync(
                    connection,
                    nameof(LauncherButtonEntity),
                    nameof(LauncherButtonEntity.ToolTip),
                    "TEXT NOT NULL DEFAULT ''");
                await AddColumnIfMissingAsync(
                    connection,
                    nameof(LauncherButtonEntity),
                    nameof(LauncherButtonEntity.LastExecutedAtUtc),
                    "datetime NULL");
                await AddColumnIfMissingAsync(
                    connection,
                    nameof(LauncherButtonEntity),
                    nameof(LauncherButtonEntity.SortOrder),
                    "INTEGER NOT NULL DEFAULT 0");
                break;
            default:
                throw new NotSupportedException($"Unknown schema migration target version: {targetVersion}");
        }
    }

    private static async Task AddColumnIfMissingAsync(
        SQLiteAsyncConnection connection,
        string tableName,
        string columnName,
        string columnDefinition)
    {
        if (await ColumnExistsAsync(connection, tableName, columnName))
        {
            return;
        }

        await connection.ExecuteAsync($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};");
    }

    private static async Task<bool> ColumnExistsAsync(
        SQLiteAsyncConnection connection,
        string tableName,
        string columnName)
    {
        var count = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM pragma_table_info('{tableName}') WHERE name = ?;",
            columnName);
        return count > 0;
    }

    private async Task<IReadOnlyList<Guid>> GetDockButtonIdsCoreAsync()
    {
        var setting = await Connection.FindAsync<AppSettingEntity>(DockOrderKey);
        return DockOrderValueCodec.Parse(setting?.Value);
    }

    private async Task SetDockButtonIdsCoreAsync(IReadOnlyList<Guid> ids)
    {
        var value = DockOrderValueCodec.Serialize(ids);
        await Connection.InsertOrReplaceAsync(new AppSettingEntity
        {
            Key = DockOrderKey,
            Value = value,
        });
    }

    private SQLiteAsyncConnection Connection
        => connection ?? throw new InvalidOperationException("Repository is not initialized.");
}
