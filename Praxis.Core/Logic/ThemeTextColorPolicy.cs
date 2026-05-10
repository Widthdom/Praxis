namespace Praxis.Core.Logic;

public static class ThemeTextColorPolicy
{
    public static string ResolveTextColorHex(bool isDarkTheme)
        => isDarkTheme ? "#F2F2F2" : "#111111";
}
