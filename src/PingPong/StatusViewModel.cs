using System;
using System.Windows.Controls;
using System.Windows.Input;
using Caliburn.Micro;
using PingPong.Core;
using PingPong.Models;

namespace PingPong
{
    public class StatusViewModel : Screen
    {
        private readonly TwitterClient _client;
        private readonly TweetParser _tweetParser;
        private readonly IWindowManager _windowManager;
        private string _statusText;
        private bool _isOpen;
        private OutgoingContext _outgoing;

        public string StatusText
        {
            get { return _statusText; }
            set { this.SetValue("StatusText", value, ref _statusText); }
        }

        public bool IsOpen
        {
            get { return _isOpen; }
            set
            {
                if (this.SetValue("IsOpen", value, ref _isOpen) && value)
                    _windowManager.ShowPopup(this);
            }
        }

        public StatusViewModel(TwitterClient client, TweetParser tweetParser, IWindowManager windowManager)
        {
            _client = client;
            _tweetParser = tweetParser;
            _windowManager = windowManager;
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
            IsOpen = false;
        }

        public void OnStatusTextBoxTextInput(TextCompositionEventArgs e)
        {
            if (e.Text[0] == 27) // esc
                TryClose();
        }

        public void OnStatusTextBoxChanged(TextBox sender, TextChangedEventArgs e)
        {
            int length;
            string text = sender.Text;
            _tweetParser.Parse(sender.Text, out length);
            if (text.Contains("\r") && length <= TweetParser.MaxLength)
            {
                text = text.Replace("\r", "").Replace("\n", "");
                if (_outgoing != null)
                {
                    switch (_outgoing.Type)
                    {
                        case OutgoingType.Reply:
                            // TODO: check text has screen name
                            _client.UpdateStatus(text, _outgoing.Tweet.Id);
                            break;
                        case OutgoingType.Retweet:
                            _client.Retweet(_outgoing.Tweet.Id);
                            break;
                        case OutgoingType.Quote:
                            _client.UpdateStatus(text, _outgoing.Tweet.Id);
                            break;
                        case OutgoingType.DirectMessage:
                            _client.DirectMessage(_outgoing.ScreenName, text);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    _client.UpdateStatus(text);
                }

                _outgoing = null;
                TryClose();
            }
            else if (_outgoing != null && _outgoing.Type == OutgoingType.Retweet)
            {
                _outgoing.Type = OutgoingType.Quote;
            }
        }

        public void Reply(Tweet tweet)
        {
            _outgoing = new OutgoingContext { Tweet = tweet, Type = OutgoingType.Reply };
            StatusText = '@' + tweet.ScreenName;
            IsOpen = true;
        }

        public void Retweet(Tweet tweet)
        {
            _outgoing = new OutgoingContext { Tweet = tweet, Type = OutgoingType.Retweet };
            StatusText = string.Format("RT @{0} {1}", tweet.ScreenName, tweet.Text);
            IsOpen = true;
        }

        public void Quote(Tweet tweet)
        {
            _outgoing = new OutgoingContext { Tweet = tweet, Type = OutgoingType.Quote };
            StatusText = string.Format("RT @{0} {1}", tweet.ScreenName, tweet.Text);
            IsOpen = true;
        }

        public void DirectMessage(Tweet tweet)
        {
            _outgoing = new OutgoingContext { ScreenName = tweet.ScreenName, Type = OutgoingType.DirectMessage };
            StatusText = string.Empty;
            IsOpen = true;
        }
    }

    /// <summary>Holds metadata for the next status of the user.</summary>
    internal class OutgoingContext
    {
        public Tweet Tweet { get; set; }
        public OutgoingType Type { get; set; }
        public string ScreenName { get; set; }
    }

    internal enum OutgoingType
    {
        Reply,
        Retweet,
        Quote,
        DirectMessage,
    }
}