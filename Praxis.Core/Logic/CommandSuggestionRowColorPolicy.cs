namespace Praxis.Core.Logic;

public static class CommandSuggestionRowColorPolicy
{
    public static string ResolveBackgroundHex(bool selected, bool isDarkTheme)
    {
        if (!selected)
        {
            return "#00000000";
        }

        return isDarkTheme ? "#59636E" : "#B7C0C9";
    }
}
