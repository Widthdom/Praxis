namespace Praxis.Services;

public interface IErrorLogger
{
    void Log(Exception exception, string context);
    void LogWarning(string message, string context);
    void LogInfo(string message, string context);

    /// <summary>
    /// Drains any pending async log writes to the database.
    /// Call during graceful shutdown to avoid losing buffered entries.
    /// </summary>
    Task FlushAsync(TimeSpan timeout);
}
