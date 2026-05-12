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
        var color = values.Count > 0 && values[0] is LauncherButtonColorKey key ? key : LauncherButtonColorKey.Default;
        var selected = values.Count > 1 && values[1] is true;
        var inverted = values.Count > 2 && values[2] is true;
        var theme = values.Count > 3 && values[3] is ThemeMode mode ? mode : ThemeMode.System;
        var pointerOver = values.Count > 4 && values[4] is true;
        var light = ResolveLight(theme);

        if (inverted && selected)
        {
            return Brush(light ? "#787878" : "#A0A0A0");
        }

        if (inverted)
        {
            if (pointerOver)
            {
                return Brush(light ? "#242424" : "#F2F2F2");
            }

            return Brush(light ? "#363636" : "#FFFFFF");
        }

        if (selected)
        {
            return Brush(light ? "#DCDCDC" : "#505050");
        }

        if (pointerOver)
        {
            return color switch
            {
                LauncherButtonColorKey.Blue => Brush(light ? "#CAD6E0" : "#426C92"),
                LauncherButtonColorKey.Green => Brush(light ? "#CEDBD2" : "#477557"),
                LauncherButtonColorKey.Red => Brush(light ? "#DDCBCB" : "#8C4E55"),
                LauncherButtonColorKey.Purple => Brush(light ? "#D7D0E3" : "#6D6096"),
                LauncherButtonColorKey.Amber => Brush(light ? "#DCCFB9" : "#8A7043"),
                _ => Brush(light ? "#C8C8C8" : "#566270"),
            };
        }

        return color switch
        {
            LauncherButtonColorKey.Blue => Brush(light ? "#D7E1EA" : "#335A7D"),
            LauncherButtonColorKey.Green => Brush(light ? "#DCE7DF" : "#386247"),
            LauncherButtonColorKey.Red => Brush(light ? "#EADADB" : "#7A3F45"),
            LauncherButtonColorKey.Purple => Brush(light ? "#E4DEED" : "#5A4D82"),
            LauncherButtonColorKey.Amber => Brush(light ? "#E9E0CF" : "#765F35"),
            _ => Brush(light ? "#D4D4D4" : "#343A42"),
        };
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
