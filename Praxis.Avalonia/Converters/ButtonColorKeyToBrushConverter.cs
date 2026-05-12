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
            LauncherButtonColorKey.Blue => Brush("#335A7D"),
            LauncherButtonColorKey.Green => Brush("#386247"),
            LauncherButtonColorKey.Red => Brush("#7A3F45"),
            LauncherButtonColorKey.Purple => Brush("#5A4D82"),
            LauncherButtonColorKey.Amber => Brush("#765F35"),
            _ => Brush("#343A42"),
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static IBrush Brush(string color)
        => new SolidColorBrush(Color.Parse(color));
}
