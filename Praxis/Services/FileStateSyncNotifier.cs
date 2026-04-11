using System.Globalization;
using Praxis.Core.Logic;

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
        };

        watcher.Changed += OnSignalChanged;
        watcher.Created += OnSignalChanged;
        watcher.Renamed += OnSignalChanged;
        watcher.EnableRaisingEvents = true;
    }

    public async Task NotifyButtonsChangedAsync(CancellationToken cancellationToken = default)
    {
        var payload = $"{instanceId}|{DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture)}";
        lock (gate)
        {
            lastObservedPayload = payload;
        }

        Directory.CreateDirectory(directoryPath);
        await File.WriteAllTextAsync(signalPath, payload, cancellationToken);
        CrashFileLogger.WriteInfo(nameof(FileStateSyncNotifier), $"Signal written. Source={instanceId} Path={signalPath}");
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
        Exception? readFailure = null;
        for (var i = 0; i < 3; i++)
        {
            try
            {
                if (File.Exists(signalPath))
                {
                    payload = await File.ReadAllTextAsync(signalPath);
                }

                readFailure = null;
                break;
            }
            catch (IOException ex)
            {
                readFailure = ex;
                await Task.Delay(20);
            }
            catch (UnauthorizedAccessException ex)
            {
                readFailure = ex;
                await Task.Delay(20);
            }
        }

        payload = payload.Trim();
        if (string.IsNullOrWhiteSpace(payload))
        {
            if (readFailure is not null)
            {
                CrashFileLogger.WriteWarning(nameof(FileStateSyncNotifier), $"Failed to read sync payload after retries: {readFailure.Message}");
            }

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

        if (!StateSyncPayloadParser.TryParse(payload, out var source, out var timestamp))
        {
            CrashFileLogger.WriteWarning(nameof(FileStateSyncNotifier), $"Ignored malformed sync payload: \"{payload}\"");
            return;
        }

        if (string.Equals(source, instanceId, StringComparison.Ordinal))
        {
            return;
        }

        CrashFileLogger.WriteInfo(nameof(FileStateSyncNotifier), $"Signal observed. Source={source} TimestampUtc={timestamp:O}");

        try
        {
            ButtonsChanged?.Invoke(this, new StateSyncChangedEventArgs
            {
                SourceInstanceId = source,
                TimestampUtc = timestamp,
            });
        }
        catch (Exception ex)
        {
            CrashFileLogger.WriteException(nameof(FileStateSyncNotifier), ex);
        }
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
