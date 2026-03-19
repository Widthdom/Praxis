namespace Praxis.Core.Logic;

public static class MacCommandInputSourcePolicy
{
    private static readonly TimeSpan focusedInputSourceEnforcementInterval = TimeSpan.FromMilliseconds(120);

    public static TimeSpan FocusedInputSourceEnforcementInterval => focusedInputSourceEnforcementInterval;

    public static bool ShouldForceAsciiInputSource(
        bool isFirstResponder,
        bool isWindowKey,
        bool isAppActive,
        bool enforceAsciiInput)
        => isFirstResponder && isWindowKey && isAppActive && enforceAsciiInput;
}
