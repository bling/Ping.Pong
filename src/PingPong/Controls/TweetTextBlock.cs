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
        private readonly TweetParser _parser;

        public TweetTextBlock()
        {
            DefaultStyleKey = typeof(TweetTextBlock);

            _ea = Execute.InDesignMode ? new EventAggregator() : IoC.Get<IEventAggregator>();
            _parser = Execute.InDesignMode ? new TweetParser() : IoC.Get<TweetParser>();
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

            int totalCharacters;
            foreach (var part in _parser.Parse(Text, out totalCharacters))
            {
                string text = part.Text;
                switch (part.Type)
                {
                    case TweetPartType.Topic:
                        var topic = new Hyperlink();
                        topic.TextDecorations = null;
                        topic.Command = new DelegateCommand<string>(_ => _ea.Publish(new NavigateToTopicMessage(text)));
                        topic.Inlines.Add(text);
                        para.Inlines.Add(topic);
                        break;
                    case TweetPartType.Hyperlink:
                        var link = new Hyperlink { NavigateUri = (Uri)part.State, TargetName = "_blank" };
                        link.TextDecorations = null;
                        link.Inlines.Add(text);
                        para.Inlines.Add(link);
                        break;
                    case TweetPartType.Text:
                        para.Inlines.Add(text);
                        break;
                    case TweetPartType.User:
                        var user = new Hyperlink();
                        user.Command = new DelegateCommand<string>(_ => _ea.Publish(new NavigateToUserMessage(text)));
                        user.TextDecorations = null;
                        user.Inlines.Add(text);
                        para.Inlines.Add(user);
                        break;
                }
                para.Inlines.Add(" ");
            }
        }
    }
}