using System.Globalization;
using Praxis.Core.Logic;
using Praxis.Core.Services;
using Praxis.Data.Storage;

namespace Praxis.Avalonia.Services;

public sealed class FileStateSyncNotifier : IStateSyncNotifier
{
    private readonly string instanceId = Guid.NewGuid().ToString("N");
    private readonly string directoryPath;
    private readonly string signalPath;
    private readonly FileSystemWatcher watcher;
    private readonly object gate = new();
    private string lastObservedPayload = string.Empty;
    private bool disposed;

    public FileStateSyncNotifier()
        : this(new AppStoragePathProvider())
    {
    }

    public FileStateSyncNotifier(AppStoragePathProvider storagePathProvider)
    {
        directoryPath = storagePathProvider.AppDataDirectory;
        signalPath = Path.Combine(directoryPath, AppStoragePathLayoutResolver.ButtonsSyncFileName);
        Directory.CreateDirectory(directoryPath);

        watcher = new FileSystemWatcher(directoryPath, Path.GetFileName(signalPath))
        {
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size,
        };
        watcher.Changed += OnSignalChanged;
        watcher.Created += OnSignalChanged;
        watcher.Renamed += OnSignalChanged;
        watcher.EnableRaisingEvents = true;
    }

    public event EventHandler<StateSyncChangedEventArgs>? ButtonsChanged;

    public async Task NotifyButtonsChangedAsync(CancellationToken cancellationToken = default)
    {
        if (disposed)
        {
            return;
        }

        var payload = $"{instanceId}|{DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture)}";
        lock (gate)
        {
            lastObservedPayload = payload;
        }

        Directory.CreateDirectory(directoryPath);
        await File.WriteAllTextAsync(signalPath, payload, cancellationToken);
    }

    private void OnSignalChanged(object sender, FileSystemEventArgs e)
        => _ = TryPublishAsync();

    private async Task TryPublishAsync()
    {
        if (disposed)
        {
            return;
        }

        string payload = string.Empty;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                if (File.Exists(signalPath))
                {
                    payload = (await File.ReadAllTextAsync(signalPath)).Trim();
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

        if (!StateSyncPayloadParser.TryParse(payload, out var source, out var timestamp)
            || string.Equals(source, instanceId, StringComparison.Ordinal))
        {
            return;
        }

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
        watcher.EnableRaisingEvents = false;
        watcher.Changed -= OnSignalChanged;
        watcher.Created -= OnSignalChanged;
        watcher.Renamed -= OnSignalChanged;
        watcher.Dispose();
    }
}
