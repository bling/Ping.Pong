using System;
using System.Globalization;
using System.Windows.Data;
using PingPong.Models;

namespace PingPong.Converters
{
    public class CharactersRemainingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int length = value != null ? value.ToString().Length : 0;
            return Tweet.MaxLength - length;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}