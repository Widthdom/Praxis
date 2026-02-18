namespace Praxis.Core.Logic;

public static class ThemeShortcutModeResolver
{
    public static bool TryResolveModeFromMacKeyInput(string keyInput, out string mode)
    {
        if (string.Equals(keyInput, "l", StringComparison.OrdinalIgnoreCase))
        {
            mode = "Light";
            return true;
        }

        if (string.Equals(keyInput, "d", StringComparison.OrdinalIgnoreCase))
        {
            mode = "Dark";
            return true;
        }

        if (string.Equals(keyInput, "h", StringComparison.OrdinalIgnoreCase))
        {
            mode = "System";
            return true;
        }

        mode = string.Empty;
        return false;
    }
}
