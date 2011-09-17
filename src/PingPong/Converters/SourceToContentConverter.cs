using System;
using System.Globalization;
using System.Windows.Data;
using Caliburn.Micro;

namespace PingPong.Converters
{
    public class SourceToContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string link = value != null ? value.ToString() : string.Empty;
                if (link == "web")
                    return "via web";

                if (link.StartsWith("<a href"))
                {
                    var parts = link.Split('"');
                    var name = parts[parts.Length - 1].Split(new[] { '<', '>' }, StringSplitOptions.RemoveEmptyEntries);
                    return "via " + name[0];
                }
            }
            catch (Exception e)
            {
                LogManager.GetLog(GetType()).Error(e);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}