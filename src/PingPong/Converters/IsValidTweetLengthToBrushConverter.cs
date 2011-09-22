using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Caliburn.Micro;
using PingPong.Core;

namespace PingPong.Converters
{
    public class IsValidTweetLengthToBrushConverter : IValueConverter
    {
        private readonly TweetParser _parser;

        public Brush Positive { get; set; }
        public Brush Negative { get; set; }

        public IsValidTweetLengthToBrushConverter()
        {
            _parser = Execute.InDesignMode ? new TweetParser() : IoC.Get<TweetParser>();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string;
            if (text != null)
            {
                int count;
                _parser.Parse(text, out count);
                return count <= TweetParser.MaxLength ? Positive : Negative;
            }

            return Negative;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}