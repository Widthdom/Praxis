using System.Globalization;
using Avalonia.Data.Converters;

namespace Praxis.Avalonia.Converters;

public sealed class EmptyStringToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.IsNullOrEmpty(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
