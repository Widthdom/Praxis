namespace Praxis.Core.Models;

public readonly record struct LauncherExecutionResult(bool Succeeded, string Message)
{
    public static LauncherExecutionResult Success(string message) => new(true, message);

    public static LauncherExecutionResult Failure(string message) => new(false, message);
}
