using Microsoft.UI.Xaml.Data;
using System;

namespace AIM.Converters;

public class BoolToSymbolConverter : IValueConverter
{
    // This method converts the 'IsFolder' boolean to a Segoe Fluent Icons glyph.
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isFolder)
        {
            // Return the glyph for a Folder or a Page (file).
            return isFolder ? "\uE8B7" : "\uE7C3";
        }
        return null;
    }

    // This method is not needed for one-way binding.
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}