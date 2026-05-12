using System.Globalization;
using Avalonia.Data.Converters;

namespace Praxis.Avalonia.Converters;

public sealed class InputPlaceholderVisibleConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = values.Count > 0 ? values[0] as string : null;
        var focused = values.Count > 1 && values[1] is true;
        return !focused && string.IsNullOrEmpty(text);
    }
}
