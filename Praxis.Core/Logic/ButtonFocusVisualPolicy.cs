namespace Praxis.Core.Logic;

public static class ButtonFocusVisualPolicy
{
    public static double ResolveBorderWidth() => 1.5;

    public static string ResolveBorderColorHex(bool focused, bool isDarkTheme)
    {
        if (!focused)
        {
            return "#00000000";
        }

        return isDarkTheme ? "#F2F2F2" : "#1A1A1A";
    }
}
