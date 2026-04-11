using System.Globalization;
using Praxis.Core.Models;

namespace Praxis.Core.Logic;

public static class ThemeModeParser
{
    public static ThemeMode NormalizeOrDefault(ThemeMode value, ThemeMode defaultMode = ThemeMode.System)
    {
        defaultMode = Enum.IsDefined(defaultMode)
            ? defaultMode
            : ThemeMode.System;

        return Enum.IsDefined(value)
            ? value
            : defaultMode;
    }

    public static bool TryParse(string? value, out ThemeMode parsed)
    {
        parsed = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var candidate = value.Trim();
        if (int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            return false;
        }

        if (!Enum.TryParse<ThemeMode>(candidate, true, out var candidateMode) || !Enum.IsDefined(candidateMode))
        {
            return false;
        }

        parsed = candidateMode;
        return true;
    }

    public static ThemeMode ParseOrDefault(string? value, ThemeMode defaultMode = ThemeMode.System)
    {
        defaultMode = NormalizeOrDefault(defaultMode, ThemeMode.System);
        return TryParse(value, out var parsed)
            ? parsed
            : defaultMode;
    }
}
