using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Vimium.Converters;

/// <summary>
/// Converts a hex color string (#RRGGBB) to a SolidColorBrush.
/// Returns Transparent on invalid input.
/// </summary>
public class HexToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            var hex = value as string;
            if (string.IsNullOrEmpty(hex)) return Brushes.Transparent;

            var color = (Color)ColorConverter.ConvertFromString(hex);
            return new SolidColorBrush(color);
        }
        catch
        {
            return Brushes.Transparent;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush brush)
        {
            var c = brush.Color;
            return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }

        return "#000000";
    }
}
