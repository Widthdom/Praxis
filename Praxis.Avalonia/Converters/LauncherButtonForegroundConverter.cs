using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using Praxis.Core.Models;

namespace Praxis.Avalonia.Converters;

public sealed class LauncherButtonForegroundConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var inverted = values.Count > 0 && values[0] is true;
        var theme = values.Count > 1 && values[1] is ThemeMode mode ? mode : ThemeMode.System;
        var light = ResolveLight(theme);
        return inverted
            ? Brush(light ? "#F2F2F2" : "#1A1A1A")
            : Brush(light ? "#1A1A1A" : "#F3F6F8");
    }

    private static bool ResolveLight(ThemeMode theme)
        => theme switch
        {
            ThemeMode.Light => true,
            ThemeMode.Dark => false,
            _ => Application.Current?.ActualThemeVariant == ThemeVariant.Light
                || Application.Current?.RequestedThemeVariant == ThemeVariant.Light,
        };

    private static IBrush Brush(string color)
        => new SolidColorBrush(Color.Parse(color));
}
