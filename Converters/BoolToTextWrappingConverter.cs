using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace AIM.Converters;

public class BoolToTextWrappingConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool isEnabled && isEnabled ? TextWrapping.Wrap : TextWrapping.NoWrap;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}