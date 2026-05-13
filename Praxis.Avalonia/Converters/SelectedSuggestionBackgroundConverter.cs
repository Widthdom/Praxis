using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using Praxis.Core.Models;

namespace Praxis.Avalonia.Converters;

public sealed class SelectedSuggestionBackgroundConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var selected = values.Count > 0 && values[0] is true;
        if (!selected)
        {
            return Brushes.Transparent;
        }

        var theme = values.Count > 1 && values[1] is ThemeMode mode ? mode : ThemeMode.System;
        var light = theme switch
        {
            ThemeMode.Light => true,
            ThemeMode.Dark => false,
            _ => Application.Current?.ActualThemeVariant == ThemeVariant.Light
                || Application.Current?.RequestedThemeVariant == ThemeVariant.Light,
        };

        return new SolidColorBrush(Color.Parse(light ? "#CFCFCF" : "#555555"));
    }
}
