namespace Praxis.Core.Logic;

public static class ButtonFocusVisualPolicy
{
    // The focus signal is conveyed via background color tint, not a border ring,
    // so the button's label does not jitter on focus / unfocus.
    public static double ResolveBorderWidth() => 0;

    public static string ResolveBorderColorHex(bool focused, bool isDarkTheme) => "#00000000";

    public static string ResolveBackgroundColorHex(bool focused, bool isDarkTheme)
    {
        if (!focused)
        {
            return "#00000000";
        }

        return isDarkTheme ? "#3F4750" : "#D8DDE3";
    }
}
