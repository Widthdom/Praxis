namespace Praxis.Core.Logic;

public static class MacCommandInputSourcePolicy
{
    public static bool ShouldForceAsciiInputSource(bool isFirstResponder, bool isWindowKey, bool isAppActive)
        => isFirstResponder && isWindowKey && isAppActive;
}
