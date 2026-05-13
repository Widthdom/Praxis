using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using Praxis.Core.Models;

namespace Praxis.Avalonia.Converters;

public sealed class LauncherButtonBackgroundConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var selected = values.Count > 1 && values[1] is true;
        var inverted = values.Count > 2 && values[2] is true;
        var theme = values.Count > 3 && values[3] is ThemeMode mode ? mode : ThemeMode.System;
        var pointerOver = values.Count > 4 && values[4] is true;
        var light = ResolveLight(theme);

        if (inverted && selected)
        {
            return Brush(light ? "#404040" : "#C8C8C8");
        }

        if (inverted)
        {
            if (pointerOver)
            {
                return Brush(light ? "#444444" : "#D0D0D0");
            }

            return Brush(light ? "#565656" : "#F2F2F2");
        }

        if (selected)
        {
            return Brush(light ? "#C8C8C8" : "#404040");
        }

        if (pointerOver)
        {
            return Brush(light ? "#D0D0D0" : "#444444");
        }

        return Brush(light ? "#F2F2F2" : "#565656");
    }

    private static IBrush Brush(string color)
        => new SolidColorBrush(Color.Parse(color));

    private static bool ResolveLight(ThemeMode theme)
        => theme switch
        {
            ThemeMode.Light => true,
            ThemeMode.Dark => false,
            _ => Application.Current?.ActualThemeVariant == ThemeVariant.Light
                || Application.Current?.RequestedThemeVariant == ThemeVariant.Light,
        };
}
