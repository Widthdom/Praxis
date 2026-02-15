using System.Globalization;

namespace Praxis.Services;

public sealed class FileStateSyncNotifier : IStateSyncNotifier
{
    private readonly string instanceId = Guid.NewGuid().ToString("N");
    private readonly string directoryPath;
    private readonly string signalPath;
    private readonly FileSystemWatcher watcher;
    private readonly object gate = new();
    private string lastObservedPayload = string.Empty;
    private bool disposed;

    public event EventHandler<StateSyncChangedEventArgs>? ButtonsChanged;

    public FileStateSyncNotifier()
    {
        directoryPath = AppStoragePaths.AppDataFolderPath;
        signalPath = AppStoragePaths.ButtonsSyncPath;
        Directory.CreateDirectory(directoryPath);

        watcher = new FileSystemWatcher(directoryPath, Path.GetFileName(signalPath))
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true,
        };

        watcher.Changed += OnSignalChanged;
        watcher.Created += OnSignalChanged;
        watcher.Renamed += OnSignalChanged;
    }

    public async Task NotifyButtonsChangedAsync(CancellationToken cancellationToken = default)
    {
        var payload = $"{instanceId}|{DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture)}";
        lock (gate)
        {
            lastObservedPayload = payload;
        }

        await File.WriteAllTextAsync(signalPath, payload, cancellationToken);
    }

    private void OnSignalChanged(object sender, FileSystemEventArgs e)
    {
        _ = TryPublishAsync();
    }

    private async Task TryPublishAsync()
    {
        if (disposed)
        {
            return;
        }

        string payload = string.Empty;
        for (var i = 0; i < 3; i++)
        {
            try
            {
                if (File.Exists(signalPath))
                {
                    payload = await File.ReadAllTextAsync(signalPath);
                }
                break;
            }
            catch (IOException)
            {
                await Task.Delay(20);
            }
            catch (UnauthorizedAccessException)
            {
                await Task.Delay(20);
            }
        }

        payload = payload.Trim();
        if (string.IsNullOrWhiteSpace(payload))
        {
            return;
        }

        lock (gate)
        {
            if (string.Equals(payload, lastObservedPayload, StringComparison.Ordinal))
            {
                return;
            }

            lastObservedPayload = payload;
        }

        var parts = payload.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return;
        }

        var source = parts[0];
        if (string.Equals(source, instanceId, StringComparison.Ordinal))
        {
            return;
        }

        if (!long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
        {
            return;
        }

        var timestamp = new DateTime(ticks, DateTimeKind.Utc);
        ButtonsChanged?.Invoke(this, new StateSyncChangedEventArgs
        {
            SourceInstanceId = source,
            TimestampUtc = timestamp,
        });
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        watcher.Changed -= OnSignalChanged;
        watcher.Created -= OnSignalChanged;
        watcher.Renamed -= OnSignalChanged;
        watcher.Dispose();
    }
}
