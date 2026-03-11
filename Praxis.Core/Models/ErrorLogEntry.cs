namespace Praxis.Core.Models;

public sealed class ErrorLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Context { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
