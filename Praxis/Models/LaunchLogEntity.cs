namespace Praxis.Models;

public sealed class LaunchLogEntity
{
    [SQLite.PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? ButtonId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Tool { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
