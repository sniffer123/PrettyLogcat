using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PrettyLogcat.Converters
{
    public class BooleanToTextWrappingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TextWrapping.Wrap : TextWrapping.NoWrap;
            }
            return TextWrapping.NoWrap;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TextWrapping textWrapping)
            {
                return textWrapping == TextWrapping.Wrap;
            }
            return false;
        }
    }
}