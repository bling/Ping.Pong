using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Caliburn.Micro;
using PingPong.Messages;

namespace PingPong.Controls
{
    public class TweetTextBlock : Control
    {
        private readonly char[] PunctuationChars = new[]
        {
            '.', '?', '!'
        };

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(TweetTextBlock), new PropertyMetadata(OnTextChanged));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
		
		public static readonly DependencyProperty ScreenNameProperty =
            DependencyProperty.Register("ScreenName", typeof(string), typeof(TweetTextBlock), new PropertyMetadata(null));

        public string ScreenName
        {
            get { return (string)GetValue(ScreenNameProperty); }
            set { SetValue(ScreenNameProperty, value); }
        }

        private RichTextBox _block;
        private readonly IEventAggregator _ea;

        public TweetTextBlock()
        {
            DefaultStyleKey = typeof(TweetTextBlock);
            if (!Execute.InDesignMode)
                _ea = IoC.Get<IEventAggregator>();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _block = (RichTextBox)GetTemplateChild("PART_TextBlock");
            UpdateText();
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TweetTextBlock)d).UpdateText();
        }

        private void UpdateText()
        {
            if (_block == null || string.IsNullOrEmpty(Text))
                return;

            var para = new Paragraph();
            if (!string.IsNullOrEmpty(ScreenName)) para.Inlines.Add(ScreenName + "  ");
            
			_block.Blocks.Clear();
            _block.Blocks.Add(para);

            var parts = Text.Split(' ');
            foreach (var p in parts)
            {
                if (p.StartsWith("#"))
                {
                    var topic = p.TrimEnd(PunctuationChars);
                    var link = new Hyperlink();
                    link.TextDecorations = null;
                    link.Command = new DelegateCommand<string>(_ => _ea.Publish(new NavigateToTopicMessage(topic)));
                    link.Inlines.Add(p);
                    para.Inlines.Add(link);
                    para.Inlines.Add(" ");
                }
                else if (p.StartsWith("http://"))
                {
                    string cleanLink = p.TrimEnd(PunctuationChars);

                    Uri uri;
                    if (Uri.TryCreate(cleanLink, UriKind.RelativeOrAbsolute, out uri))
                    {
                        var link = new Hyperlink { NavigateUri = new Uri(cleanLink), TargetName = "_blank" };
                        link.TextDecorations = null;
                        link.Inlines.Add(p);
                        para.Inlines.Add(link);
                        para.Inlines.Add(" ");
                    }
                    else
                    {
                        para.Inlines.Add(p + ' ');
                    }
                }
                else if (p.StartsWith("@"))
                {
                    var user = p.Substring(1).TrimEnd(PunctuationChars);
                    var link = new Hyperlink();
                    link.Command = new DelegateCommand<string>(_ => _ea.Publish(new NavigateToUserMessage(user)));
                    link.TextDecorations = null;
                    link.Inlines.Add(p);
                    para.Inlines.Add(link);
                    para.Inlines.Add(" ");
                }
                else
                {
                    para.Inlines.Add(p + ' ');
                }
            }
        }
    }
}