namespace Praxis.Core.Logic;

public static class MacCommandInputSourcePolicy
{
    private static readonly TimeSpan focusedInputSourceEnforcementInterval = TimeSpan.FromMilliseconds(200);

    public static TimeSpan FocusedInputSourceEnforcementInterval => focusedInputSourceEnforcementInterval;

    public static bool ShouldForceAsciiInputSource(bool isFirstResponder, bool isWindowKey, bool isAppActive)
        => isFirstResponder && isWindowKey && isAppActive;
}
