using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using PingPong.Models;

namespace PingPong.Converters
{
    public class IsValidTweetLengthToBrushConverter : IValueConverter
    {
        public Brush Positive { get; set; }
        public Brush Negative { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string;
            if (text != null)
                return text.Length <= Tweet.MaxLength ? Positive : Negative;

            return Negative;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}