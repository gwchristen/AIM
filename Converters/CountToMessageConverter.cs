using Microsoft.UI.Xaml.Data;
using System;

namespace AIM.Converters
{
    public class CountToMessageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int count)
            {
                return $"Showing {count} log entries";
            }
            return "Showing 0 log entries";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}