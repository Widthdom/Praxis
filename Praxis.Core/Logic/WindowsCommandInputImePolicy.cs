namespace Praxis.Core.Logic;

public static class WindowsCommandInputImePolicy
{
    private static readonly TimeSpan focusedInputScopeEnforcementInterval = TimeSpan.FromMilliseconds(200);

    public static TimeSpan FocusedInputScopeEnforcementInterval => focusedInputScopeEnforcementInterval;

    public static bool ShouldEnforceInputScope(bool isFocused) => isFocused;

    public static int ClampSelectionStart(int selectionStart, int textLength)
    {
        if (textLength <= 0)
        {
            return 0;
        }

        if (selectionStart <= 0)
        {
            return 0;
        }

        if (selectionStart >= textLength)
        {
            return textLength;
        }

        return selectionStart;
    }
}
