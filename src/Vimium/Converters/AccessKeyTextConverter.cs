using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Documents;

namespace Vimium.Converters;

/// <summary>
/// Converts a display name to underlined first letter for access key hint.
/// E.g., "General" → "G" underlined + "eneral".
/// </summary>
public class AccessKeyTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value as string ?? "";
        if (string.IsNullOrEmpty(text)) return new Run("");

        var span = new Span();
        span.Inlines.Add(new Run(text[0].ToString()) { TextDecorations = System.Windows.TextDecorations.Underline });
        span.Inlines.Add(new Run(text[1..]));
        return span;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
