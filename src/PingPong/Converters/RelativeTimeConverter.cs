using System;
using System.Globalization;
using System.Windows.Data;

namespace PingPong.Converters
{
    public class RelativeTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var date = ((DateTime)value).ToUniversalTime();
            var diff = DateTime.UtcNow - date;

            if (diff < TimeSpan.FromSeconds(30))
                return "moments ago";

            if (diff < TimeSpan.FromMinutes(1))
                return string.Format("{0}s ago", diff.Seconds);

            if (diff < TimeSpan.FromHours(1))
                return string.Format("{0}m, {1}s ago", diff.Minutes, diff.Seconds);

            if (diff < TimeSpan.FromDays(1))
                return string.Format("{0}hr, {1}m ago", diff.Hours, diff.Minutes);

            return date.ToLocalTime().ToString("MMM dd, h:mm tt");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}