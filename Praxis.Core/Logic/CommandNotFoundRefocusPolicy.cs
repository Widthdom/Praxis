namespace Praxis.Core.Logic;

public static class CommandNotFoundRefocusPolicy
{
    public static bool ShouldRefocusMainCommand(string? statusMessage)
    {
        if (string.IsNullOrWhiteSpace(statusMessage))
        {
            return false;
        }

        return statusMessage.StartsWith("Command not found:", StringComparison.OrdinalIgnoreCase);
    }
}
