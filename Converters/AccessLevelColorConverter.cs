using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace AIM.Converters;

/// <summary>
/// Converts an access level integer to a color brush for UI display.
/// </summary>
public class AccessLevelColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int accessLevel)
        {
            return accessLevel switch
            {
                1 => new SolidColorBrush(Colors.Gray),           // Basic
                2 => new SolidColorBrush(Colors.DodgerBlue),     // Admin
                3 => new SolidColorBrush(Colors.Purple),          // SuperAdmin
                _ => new SolidColorBrush(Colors.LightGray)
            };
        }

        return new SolidColorBrush(Colors.LightGray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
