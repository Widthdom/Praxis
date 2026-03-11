namespace Praxis.Models;

public sealed class ErrorLogEntity
{
    [SQLite.PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Context { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
