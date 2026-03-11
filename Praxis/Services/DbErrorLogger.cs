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

    private async Task LogAsync(Exception exception, string context)
    {
        try
        {
            var entry = new ErrorLogEntry
            {
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
            // ログ書き込み失敗は握り潰す（無限ループ防止）
        }
    }
}
