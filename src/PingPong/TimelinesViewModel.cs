using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using Autofac.Features.OwnedInstances;
using Caliburn.Micro;
using PingPong.Core;
using PingPong.Messages;
using PingPong.Models;

namespace PingPong
{
    public class TimelinesViewModel : Screen, IHandle<NavigateToUserMessage>, IHandle<NavigateToTopicMessage>
    {
        private static readonly TimeSpan StreamThrottleRate = TimeSpan.FromSeconds(20);

        private readonly TwitterClient _client;
        private readonly IWindowManager _windowManager;
        private readonly Func<Owned<TweetCollection>> _timelineFactory;
        private readonly IDisposable _refreshSubscription;
        private IDisposable _streamingSubscription;
        private string _searchText;
        private string _statusText;
        private bool _showUpdateStatus;
        private DateTime _streamStartTime = DateTime.MinValue;
        private OutgoingContext _outgoing;

        public ObservableCollection<object> Timelines { get; private set; }

        public bool ShowUpdateStatus
        {
            get { return _showUpdateStatus; }
            set { this.SetValue("ShowUpdateStatus", value, ref _showUpdateStatus); }
        }

        public string SearchText
        {
            get { return _searchText; }
            set { this.SetValue("SearchText", value, ref _searchText); }
        }

        public string StatusText
        {
            get { return _statusText; }
            set { this.SetValue("StatusText", value, ref _statusText); }
        }

        public TimelinesViewModel(TwitterClient client, IWindowManager windowManager, Func<Owned<TweetCollection>> timelineFactory)
        {
            _client = client;
            _windowManager = windowManager;
            _timelineFactory = timelineFactory;

            Timelines = new ObservableCollection<object>();
            Timelines.CollectionChanged += (sender, e) =>
            {
                if (e.OldItems != null)
                    e.OldItems.Cast<Owned<TweetCollection>>().ForEach(t => t.Dispose());
            };

            Add(timelineFactory(), tl =>
            {
                tl.Tag = StatusType.Home;
                tl.Description = "Home";
                tl.Subscribe(client.GetPollingStatuses(StatusType.Home));
            });
            Add(timelineFactory(), tl =>
            {
                tl.Tag = StatusType.Mentions;
                tl.Description = "Mentions";
                tl.Subscribe(client.GetPollingStatuses(StatusType.Mentions));
            });

            _refreshSubscription = Observable.Interval(TimeSpan.FromSeconds(30))
                .DispatcherSubscribe(_ =>
                {
                    foreach (var tweet in Timelines.Cast<dynamic>().Select(x => (TweetCollection)x.Value).SelectMany(x => x))
                        tweet.NotifyOfPropertyChange("CreatedAt");
                });
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
            Stop();
            _refreshSubscription.Dispose();
        }

        public void OnStatusTextBoxTextInput(TextCompositionEventArgs e)
        {
            if (e.Text[0] == 27) // esc
                ShowUpdateStatus = false;
        }

        public void OnStatusTextBoxChanged(TextBox sender, TextChangedEventArgs e)
        {
            string text = sender.Text;
            if (text.Contains("\r") && text.Length <= TweetParser.MaxLength)
            {
                ShowUpdateStatus = false;

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
            }
            else if (_outgoing != null && _outgoing.Type == OutgoingType.Retweet)
            {
                _outgoing.Type = OutgoingType.Quote;
            }
        }

        public void Search()
        {
            if (DateTime.UtcNow - _streamStartTime < StreamThrottleRate)
            {
                _windowManager.ShowDialog(new ErrorViewModel("You are initiating too many connections in a short period of time.  Twitter doesn't like that :("));
            }
            else if (string.IsNullOrEmpty(SearchText))
            {
                _windowManager.ShowDialog(new ErrorViewModel("Search terms are required."));
            }
            else
            {
                _streamStartTime = DateTime.UtcNow;

                Timelines.Cast<Owned<TweetCollection>>()
                    .Where(line => line.Value.Tag is string[])
                    .ToArray()
                    .ForEach(t => Timelines.Remove(t));

                var allTerms = SearchText.Split(' ', ',', ';', '|');
                var allParts = SearchText.Split(' ', ',', ';');
                var ob = _client.GetStreamingFilter(allTerms).Publish();

                foreach (string part in allParts)
                {
                    string[] terms = part.Split('|');
                    var line = _timelineFactory();
                    line.Value.Tag = terms;
                    line.Value.Description = part;
                    var sub = ob.Where(t => terms.Any(term => t.Text.Contains(term)));
                    Add(line, tl => tl.Subscribe(sub));
                }

                _streamingSubscription.DisposeIfNotNull();
                _streamingSubscription = ob.Connect();
            }
        }

        public void Stop()
        {
            _streamingSubscription.DisposeIfNotNull();
        }

        private void Add<T>(Owned<T> owned, Action<TweetCollection> setup) where T : TweetCollection
        {
            var line = (TweetCollection)((dynamic)owned).Value;
            line.OnError += ex => _windowManager.ShowDialog(new ErrorViewModel(ex.ToString()));
            setup(line);
            Timelines.Add(owned);
        }

        public void ReplyTo(Tweet tweet)
        {
            _outgoing = new OutgoingContext { Tweet = tweet, Type = OutgoingType.Reply };
            StatusText = '@' + tweet.ScreenName;
            ShowUpdateStatus = true;
        }

        public void Retweet(Tweet tweet)
        {
            _outgoing = new OutgoingContext { Tweet = tweet, Type = OutgoingType.Retweet };
            StatusText = string.Format("RT @{0} {1}", tweet.ScreenName, tweet.Text);
            ShowUpdateStatus = true;
        }

        public void Quote(Tweet tweet)
        {
            _outgoing = new OutgoingContext { Tweet = tweet, Type = OutgoingType.Quote };
            StatusText = string.Format("RT @{0} {1}", tweet.ScreenName, tweet.Text);
            ShowUpdateStatus = true;
        }

        public void DirectMessage(Tweet tweet)
        {
            _outgoing = new OutgoingContext { ScreenName = tweet.ScreenName, Type = OutgoingType.DirectMessage };
            StatusText = string.Empty;
            ShowUpdateStatus = true;
        }

        void IHandle<NavigateToUserMessage>.Handle(NavigateToUserMessage message)
        {
            var collection = _timelineFactory();
            collection.Value.Description = "@" + message.User;
            Add(collection, tl => tl.Subscribe(_client.GetPollingUserTimeline(message.User)));
        }

        void IHandle<NavigateToTopicMessage>.Handle(NavigateToTopicMessage message)
        {
            var collection = _timelineFactory();
            collection.Value.Description = "#" + message.Topic;
            Add(collection, tl => tl.Subscribe(_client.GetPollingSearch(message.Topic)));
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