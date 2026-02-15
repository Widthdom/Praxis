namespace Praxis.Services;

public interface IStateSyncNotifier : IDisposable
{
    event EventHandler<StateSyncChangedEventArgs>? ButtonsChanged;
    Task NotifyButtonsChangedAsync(CancellationToken cancellationToken = default);
}

public sealed class StateSyncChangedEventArgs : EventArgs
{
    public required string SourceInstanceId { get; init; }
    public required DateTime TimestampUtc { get; init; }
}
