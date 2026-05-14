using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Praxis.Core.Models;

namespace Praxis.Avalonia.Converters;

public sealed class ButtonColorKeyToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            LauncherButtonColorKey.Blue => Brush("#444444"),
            LauncherButtonColorKey.Green => Brush("#505050"),
            LauncherButtonColorKey.Red => Brush("#5C5C5C"),
            LauncherButtonColorKey.Purple => Brush("#686868"),
            LauncherButtonColorKey.Amber => Brush("#747474"),
            _ => Brush("#343434"),
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static IBrush Brush(string color)
        => new SolidColorBrush(Color.Parse(color));
}
