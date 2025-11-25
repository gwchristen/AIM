using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace AIM.Converters;

public class HeaderColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string headerText)
        {
            // Color code based on header name
            if (headerText.Contains("Ohio", StringComparison.OrdinalIgnoreCase))
            {
                return new SolidColorBrush(Colors.Red); // Red for Ohio
            }
            else if (headerText.Contains("I&M", StringComparison.OrdinalIgnoreCase) ||
                     headerText.Contains("IM", StringComparison.OrdinalIgnoreCase))
            {
                return new SolidColorBrush(Colors.MediumBlue); // Blue for I&M
            }
        }

        return new SolidColorBrush(Colors.Black); // Default to black
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}