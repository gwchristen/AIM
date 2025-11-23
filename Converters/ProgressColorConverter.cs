using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace AIM.Converters;

public class ProgressColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double percentage)
        {
            if (percentage < 33)
                return new SolidColorBrush(Colors.Red);
            else if (percentage < 66)
                return new SolidColorBrush(Colors.Orange);
            else
                return new SolidColorBrush(Colors.Green);
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}