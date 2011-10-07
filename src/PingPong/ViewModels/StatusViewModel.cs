using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Caliburn.Micro;
using PingPong.Core;
using PingPong.Models;

namespace PingPong.ViewModels
{
    public class StatusViewModel : Screen
    {
        private readonly TwitterClient _client;
        private readonly TweetParser _tweetParser;
        private readonly IWindowManager _windowManager;
        private string _statusText;
        private readonly OutgoingContext _outgoing = new OutgoingContext();

        public string StatusText
        {
            get { return _statusText; }
            set { this.SetValue("StatusText", value, ref _statusText); }
        }

        public StatusViewModel(TwitterClient client, TweetParser tweetParser, IWindowManager windowManager)
        {
            _client = client;
            _tweetParser = tweetParser;
            _windowManager = windowManager;
        }

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            ((FrameworkElement)view).LostFocus += OnViewLostFocus;
        }

        private void OnViewLostFocus(object sender, RoutedEventArgs e)
        {
            ((FrameworkElement)sender).LostFocus -= OnViewLostFocus;
            TryClose();
        }

        public void OnStatusTextBoxTextInput(TextCompositionEventArgs e)
        {
            switch ((int)e.Text[0])
            {
                case 27: // esc
                    TryClose();
                    break;
                case 13: // \r
                    if (_outgoing.Type == OutgoingType.Retweet)
                    {
                        _client.Retweet(_outgoing.Tweet.Id);
                    }
                    else
                    {
                        var tb = (TextBox)e.OriginalSource;
                        string text = tb.Text;
                        int length;
                        _tweetParser.Parse(text, out length);

                        if (length <= TweetParser.MaxLength)
                        {
                            switch (_outgoing.Type)
                            {
                                case OutgoingType.Reply:
                                    // TODO: check text has screen name
                                    _client.UpdateStatus(text, _outgoing.Tweet.Id);
                                    break;
                                case OutgoingType.Quote:
                                    _client.UpdateStatus(text, _outgoing.Tweet.Id);
                                    break;
                                case OutgoingType.DirectMessage:
                                    _client.DirectMessage(_outgoing.Tweet.User.ScreenName, text);
                                    break;
                                default:
                                    _client.UpdateStatus(text);
                                    break;
                            }
                        }
                    }

                    _outgoing.Type = OutgoingType.None;
                    StatusText = string.Empty;
                    TryClose();
                    break;
                default:
                    if (_outgoing.Type == OutgoingType.Retweet)
                        _outgoing.Type = OutgoingType.Quote;

                    break;
            }
        }

        public void Reply(Tweet tweet)
        {
            StatusText = '@' + tweet.User.ScreenName;
            _outgoing.Tweet = tweet;
            _outgoing.Type = OutgoingType.Reply;
            Show();
        }

        public void Retweet(Tweet tweet)
        {
            StatusText = string.Format("RT @{0} {1}", tweet.User.ScreenName, tweet.Text);
            _outgoing.Tweet = tweet;
            _outgoing.Type = OutgoingType.Retweet;
            Show();
        }

        public void Quote(Tweet tweet)
        {
            StatusText = string.Format("RT @{0} {1}", tweet.User.ScreenName, tweet.Text);
            _outgoing.Tweet = tweet;
            _outgoing.Type = OutgoingType.Quote;
            Show();
        }

        public void DirectMessage(Tweet tweet)
        {
            StatusText = string.Empty;
            _outgoing.Tweet = tweet;
            _outgoing.Type = OutgoingType.DirectMessage;
            Show();
        }

        public void Show()
        {
            // ensure popup stays within window
            double x = Math.Min(Mouse.Position.X, Application.Current.MainWindow.Width - 350);
            double y = Math.Min(Mouse.Position.Y, Application.Current.MainWindow.Height - 120);
            var settings = new Dictionary<string, object>
            {
                { "HorizontalOffset", x },
                { "VerticalOffset", y }
            };
            _windowManager.ShowPopup(this, null, settings);
        }

        /// <summary>Holds metadata for the next status of the user.</summary>
        private class OutgoingContext
        {
            public Tweet Tweet { get; set; }
            public OutgoingType Type { get; set; }
        }

        private enum OutgoingType
        {
            None,
            Reply,
            Retweet,
            Quote,
            DirectMessage,
        }
    }
}