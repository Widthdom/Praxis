namespace Praxis.Core.Logic;

public static class StatusFlashErrorPolicy
{
    public static bool IsErrorStatus(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        return message.StartsWith("Failed", StringComparison.OrdinalIgnoreCase)
               || message.Contains("error", StringComparison.OrdinalIgnoreCase)
               || message.Contains("exception", StringComparison.OrdinalIgnoreCase)
               || message.Contains("not found", StringComparison.OrdinalIgnoreCase);
    }
}
