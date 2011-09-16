using System;
using System.Globalization;
using System.Windows.Data;

namespace PingPong.Converters
{
    public class SourceToUriConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string link = value.ToString();
            if (link == "web")
                return new Uri("http://www.twitter.com");

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}