using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

namespace Vimium.Converters;

/// <summary>
/// Converts a display name to a TextBlock with underlined first letter
/// as a visual access key hint. E.g. "General" → <u>G</u>eneral.
/// </summary>
public class AccessKeyTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value as string ?? "";
        if (string.IsNullOrEmpty(text))
            return new TextBlock();

        var tb = new TextBlock();
        tb.Inlines.Add(new Run(text[0].ToString()) { TextDecorations = TextDecorations.Underline });
        tb.Inlines.Add(new Run(text[1..]));
        return tb;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
