using Praxis.Core.Models;

namespace Praxis.Core.Logic;

public static class ThemeModeParser
{
    public static ThemeMode ParseOrDefault(string? value, ThemeMode defaultMode = ThemeMode.System)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultMode;
        }

        return Enum.TryParse<ThemeMode>(value.Trim(), true, out var parsed)
            ? parsed
            : defaultMode;
    }
}
