using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace AIM.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // Handle bool
        if (value is bool b)
        {
            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        // Handle int (visible if > 0)
        if (value is int i)
        {
            return i > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // Handle string (visible if not null or empty)
        if (value is string s)
        {
            return !string.IsNullOrEmpty(s) ? Visibility.Visible : Visibility.Collapsed;
        }

        // Handle null
        if (value == null)
        {
            return Visibility.Collapsed;
        }

        // Default: visible if not null
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

// Converts true to Collapsed and false to Visible.
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // Handle bool
        if (value is bool b)
        {
            return b ? Visibility.Collapsed : Visibility.Visible;
        }

        // Handle int (collapsed if > 0)
        if (value is int i)
        {
            return i > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        // Handle string (collapsed if not null or empty)
        if (value is string s)
        {
            return !string.IsNullOrEmpty(s) ? Visibility.Collapsed : Visibility.Visible;
        }

        // Handle null - show it (inverse logic)
        if (value == null)
        {
            return Visibility.Visible;
        }

        // Default: collapsed if not null
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}