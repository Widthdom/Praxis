namespace Praxis.Services;

public interface ICommandExecutor
{
    Task<(bool Success, string Message)> ExecuteAsync(string tool, string arguments, CancellationToken cancellationToken = default);
}
