using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using Praxis.Core.Models;

namespace Praxis.Avalonia.Converters;

public sealed class StatusKindToBrushConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var kind = values.Count > 0 && values[0] is LauncherStatusKind statusKind
            ? statusKind
            : LauncherStatusKind.Idle;
        var theme = values.Count > 1 && values[1] is ThemeMode mode ? mode : ThemeMode.System;
        var light = theme switch
        {
            ThemeMode.Light => true,
            ThemeMode.Dark => false,
            _ => Application.Current?.ActualThemeVariant == ThemeVariant.Light
                || Application.Current?.RequestedThemeVariant == ThemeVariant.Light,
        };

        return kind switch
        {
            LauncherStatusKind.Success => Brush(light ? "#CFEFD8" : "#1F5B3B"),
            LauncherStatusKind.Error => Brush(light ? "#F2D1D1" : "#713333"),
            LauncherStatusKind.Busy => Brush(light ? "#F0E5BC" : "#66572B"),
            _ => Brush(light ? "#ECECEC" : "#282828"),
        };
    }

    public static IBrush Foreground(ThemeMode theme)
    {
        var light = theme switch
        {
            ThemeMode.Light => true,
            ThemeMode.Dark => false,
            _ => Application.Current?.ActualThemeVariant == ThemeVariant.Light
                || Application.Current?.RequestedThemeVariant == ThemeVariant.Light,
        };
        return Brush(light ? "#1A1A1A" : "#F4F4F4");
    }

    private static IBrush Brush(string color)
        => new SolidColorBrush(Color.Parse(color));
}
