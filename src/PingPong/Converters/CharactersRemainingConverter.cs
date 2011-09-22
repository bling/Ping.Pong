using System;
using System.Globalization;
using System.Windows.Data;
using Caliburn.Micro;
using PingPong.Core;

namespace PingPong.Converters
{
    public class CharactersRemainingConverter : IValueConverter
    {
        private readonly TweetParser _parser;

        public CharactersRemainingConverter()
        {
            _parser = Execute.InDesignMode ? new TweetParser() : IoC.Get<TweetParser>();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int length = 0;
            var text = value as string;
            if (text != null)
                _parser.Parse(text, out length);

            return TweetParser.MaxLength - length;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}