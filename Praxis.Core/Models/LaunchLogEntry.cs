namespace Praxis.Core.Models;

public sealed class LaunchLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ButtonId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Tool { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
