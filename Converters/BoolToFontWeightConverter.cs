using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace AIM.Converters;

public class BoolToFontWeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (bool)value ? FontWeights.Bold : FontWeights.Normal;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}