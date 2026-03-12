using Praxis.Core.Models;

namespace Praxis.Services;

public sealed class DbErrorLogger : IErrorLogger
{
    private const int RetentionDays = 30;

    private readonly IAppRepository repository;

    public DbErrorLogger(IAppRepository repository)
    {
        this.repository = repository;
    }

    public void Log(Exception exception, string context)
    {
        _ = LogAsync(exception, context);
    }

    public void LogInfo(string message, string context)
    {
        _ = LogInfoAsync(message, context);
    }

    private async Task LogAsync(Exception exception, string context)
    {
        try
        {
            var entry = new ErrorLogEntry
            {
                Level = "Error",
                Context = context,
                ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
                Message = exception.Message,
                StackTrace = exception.StackTrace ?? string.Empty,
                TimestampUtc = DateTime.UtcNow,
            };

            await repository.AddErrorLogAsync(entry);
            await repository.PurgeOldErrorLogsAsync(RetentionDays);
        }
        catch
        {
            // Swallow write failures to avoid infinite logging loops.
        }
    }

    private async Task LogInfoAsync(string message, string context)
    {
        try
        {
            var entry = new ErrorLogEntry
            {
                Level = "Info",
                Context = context,
                ExceptionType = string.Empty,
                Message = message,
                StackTrace = string.Empty,
                TimestampUtc = DateTime.UtcNow,
            };

            await repository.AddErrorLogAsync(entry);
        }
        catch
        {
            // Swallow write failures to avoid infinite logging loops.
        }
    }
}
