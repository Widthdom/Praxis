namespace Praxis.Core.Logic;

public static class ButtonFocusVisualPolicy
{
    // The focus signal is conveyed via background color tint, not a border ring,
    // so the button's label does not jitter on focus / unfocus.
    public static double ResolveBorderWidth() => 0;

    public static string ResolveBorderColorHex(bool focused, bool isDarkTheme) => "#00000000";

    // Returns the focus-tint hex when focused. When unfocused this returns
    // "#00000000" as a sentinel — callers must NOT paint the unfocused tint
    // directly. Instead they should clear `BackgroundColor` so the platform
    // default fill (or whatever the XAML idle binding supplies) shows
    // through; otherwise the button visually disappears against the modal
    // / popup surface it sits on.
    //
    // Focus tints must be visibly distinct from the default Button idle fill in
    // Resources/Styles/Styles.xaml (`Light=#E6E6E6, Dark=#3A3A3A`). A slightly
    // darker gray on light theme and a slightly lighter gray on dark theme
    // gives a noticeable shift without competing with the modal accent.
    public static string ResolveBackgroundColorHex(bool focused, bool isDarkTheme)
    {
        if (!focused)
        {
            return "#00000000";
        }

        return isDarkTheme ? "#5A5A5A" : "#C8C8C8";
    }
}
