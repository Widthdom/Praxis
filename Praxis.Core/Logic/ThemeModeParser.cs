using System.Globalization;
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

        var candidate = value.Trim();
        if (int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            return defaultMode;
        }

        return Enum.TryParse<ThemeMode>(candidate, true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : defaultMode;
    }
}
