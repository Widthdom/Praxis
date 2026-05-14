using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Praxis.Core.Models;

namespace Praxis.Avalonia.Converters;

public sealed class StatusForegroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is ThemeMode mode
            ? StatusKindToBrushConverter.Foreground(mode)
            : Brushes.White;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
