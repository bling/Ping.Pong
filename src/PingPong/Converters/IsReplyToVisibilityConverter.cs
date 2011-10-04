using PingPong.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PingPong.Converters
{
    public class IsReplyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Tweet)
                return string.IsNullOrWhiteSpace(((Tweet)value).InReplyToStatusId) ? Visibility.Collapsed : Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}