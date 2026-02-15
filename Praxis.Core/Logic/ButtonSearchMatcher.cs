using Praxis.Core.Models;

namespace Praxis.Core.Logic;

public static class ButtonSearchMatcher
{
    public static bool IsMatch(LauncherButtonRecord button, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var q = query.Trim();
        return Contains(button.Command, q)
            || Contains(button.ButtonText, q)
            || Contains(button.Tool, q)
            || Contains(button.Arguments, q)
            || Contains(button.ClipText, q)
            || Contains(button.Note, q);
    }

    private static bool Contains(string source, string query)
        => source?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
}
