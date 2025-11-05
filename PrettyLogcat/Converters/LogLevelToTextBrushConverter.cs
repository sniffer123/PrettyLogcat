using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using PrettyLogcat.Models;

namespace PrettyLogcat.Converters
{
    public class LogLevelToTextBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LogLevel level)
            {
                return level switch
                {
                    LogLevel.Error => new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36)), // Red for Error
                    LogLevel.Fatal => new SolidColorBrush(Color.FromRgb(0x9C, 0x27, 0xB0)), // Purple for Fatal
                    _ => new SolidColorBrush(Colors.Black) // Default black text
                };
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}