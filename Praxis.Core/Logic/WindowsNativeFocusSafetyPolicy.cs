namespace Praxis.Core.Logic;

public static class WindowsNativeFocusSafetyPolicy
{
    public static bool ShouldApplyNativeFocus(
        bool hasTextBox,
        bool isLoaded,
        bool hasXamlRoot)
    {
        return hasTextBox && isLoaded && hasXamlRoot;
    }
}
