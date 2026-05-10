namespace Praxis.Core.Logic;

public static class ThemeTextColorPolicy
{
    public static string ResolveTextColorHex(bool isDarkTheme)
        => isDarkTheme ? "#FFFFFF" : "#000000";
}
