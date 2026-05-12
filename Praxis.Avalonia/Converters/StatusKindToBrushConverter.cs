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
            LauncherStatusKind.Success => Brush(light ? "#DCE8E0" : "#254A38"),
            LauncherStatusKind.Error => Brush(light ? "#E9DDE0" : "#593038"),
            LauncherStatusKind.Busy => Brush(light ? "#DDE1EB" : "#343D55"),
            _ => Brush(light ? "#ECECEC" : "#22272D"),
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
        return Brush(light ? "#1A1A1A" : "#F3F6F8");
    }

    private static IBrush Brush(string color)
        => new SolidColorBrush(Color.Parse(color));
}
