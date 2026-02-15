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
        dbPath = AppStoragePaths.DatabasePath;
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

            connection = new SQLiteAsyncConnection(dbPath);
            await connection.CreateTableAsync<LauncherButtonEntity>();
            await connection.CreateTableAsync<LaunchLogEntity>();
            await connection.CreateTableAsync<AppSettingEntity>();

            var entities = await connection.Table<LauncherButtonEntity>().ToListAsync();
            cache = entities.Select(Map).ToList();
            RebuildCommandCache();

        }
        finally
        {
            gate.Release();
        }
    }

    public Task<IReadOnlyList<LauncherButtonRecord>> GetButtonsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<LauncherButtonRecord>>(cache.Select(Clone).ToList());

    public async Task<IReadOnlyList<LauncherButtonRecord>> ReloadButtonsAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var entities = await connection!.Table<LauncherButtonEntity>().ToListAsync();
        cache = entities.Select(Map).ToList();
        RebuildCommandCache();
        return cache.Select(Clone).ToList();
    }

    public async Task<LauncherButtonRecord?> GetByIdAsync(Guid id, bool forceReload = false, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        if (forceReload)
        {
            var entity = await connection!.FindAsync<LauncherButtonEntity>(id.ToString());
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

            RebuildCommandCache();
            return Clone(mapped);
        }

        var cached = cache.FirstOrDefault(x => x.Id == id);
        return cached is null ? null : Clone(cached);
    }

    public Task<LauncherButtonRecord?> GetByCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return Task.FromResult<LauncherButtonRecord?>(null);
        }

        commandCache.TryGetValue(command, out var match);
        return Task.FromResult(match is null ? null : Clone(match));
    }

    public async Task UpsertButtonAsync(LauncherButtonRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        await InitializeAsync(cancellationToken);

        var entity = Map(record);
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await connection!.InsertOrReplaceAsync(entity);

        var existing = cache.FindIndex(x => x.Id == record.Id);
        if (existing >= 0)
        {
            cache[existing] = Clone(Map(entity));
        }
        else
        {
            cache.Add(Clone(Map(entity)));
        }

        RebuildCommandCache();
    }

    public async Task DeleteButtonAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await connection!.DeleteAsync<LauncherButtonEntity>(id.ToString());
        cache.RemoveAll(x => x.Id == id);
        RebuildCommandCache();
    }

    public async Task AddLogAsync(LaunchLogEntry entry, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
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

        await connection!.InsertAsync(entity);
    }

    public async Task PurgeOldLogsAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        if (retentionDays < 1)
        {
            retentionDays = 1;
        }

        var threshold = DateTime.UtcNow.AddDays(-retentionDays);
        await connection!.ExecuteAsync(
            $"DELETE FROM {nameof(LaunchLogEntity)} WHERE {nameof(LaunchLogEntity.TimestampUtc)} < ?",
            threshold);
    }

    public async Task SetThemeAsync(ThemeMode themeMode, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await connection!.InsertOrReplaceAsync(new AppSettingEntity
        {
            Key = ThemeKey,
            Value = themeMode.ToString(),
        });
    }

    public async Task<ThemeMode> GetThemeAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var setting = await connection!.FindAsync<AppSettingEntity>(ThemeKey);
        if (setting is null || !Enum.TryParse<ThemeMode>(setting.Value, true, out var mode))
        {
            return ThemeMode.System;
        }

        return mode;
    }

    public async Task<IReadOnlyList<Guid>> GetDockButtonIdsAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var setting = await connection!.FindAsync<AppSettingEntity>(DockOrderKey);
        if (setting is null || string.IsNullOrWhiteSpace(setting.Value))
        {
            return [];
        }

        var ids = new List<Guid>();
        var parts = setting.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            if (Guid.TryParse(part, out var id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    public async Task SetDockButtonIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var value = string.Join(",", ids.Select(x => x.ToString()));
        await connection!.InsertOrReplaceAsync(new AppSettingEntity
        {
            Key = DockOrderKey,
            Value = value,
        });
    }

    private static LauncherButtonRecord Map(LauncherButtonEntity x)
        => new()
        {
            Id = Guid.Parse(x.Id),
            Command = x.Command,
            ButtonText = x.ButtonText,
            Tool = x.Tool,
            Arguments = x.Arguments,
            ClipText = x.ClipText,
            Note = x.Note,
            X = x.X,
            Y = x.Y,
            Width = x.Width,
            Height = x.Height,
            CreatedAtUtc = x.CreatedAtUtc,
            UpdatedAtUtc = x.UpdatedAtUtc,
        };

    private static LauncherButtonEntity Map(LauncherButtonRecord x)
        => new()
        {
            Id = x.Id.ToString(),
            Command = x.Command,
            ButtonText = x.ButtonText,
            Tool = x.Tool,
            Arguments = x.Arguments,
            ClipText = x.ClipText,
            Note = x.Note,
            X = x.X,
            Y = x.Y,
            Width = x.Width,
            Height = x.Height,
            CreatedAtUtc = x.CreatedAtUtc,
            UpdatedAtUtc = x.UpdatedAtUtc,
        };

    private static LauncherButtonRecord Clone(LauncherButtonRecord x)
        => new()
        {
            Id = x.Id,
            Command = x.Command,
            ButtonText = x.ButtonText,
            Tool = x.Tool,
            Arguments = x.Arguments,
            ClipText = x.ClipText,
            Note = x.Note,
            X = x.X,
            Y = x.Y,
            Width = x.Width,
            Height = x.Height,
            CreatedAtUtc = x.CreatedAtUtc,
            UpdatedAtUtc = x.UpdatedAtUtc,
        };

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


