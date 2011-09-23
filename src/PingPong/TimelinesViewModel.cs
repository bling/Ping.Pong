using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
        private readonly TweetParser _tweetParser;
        private readonly IWindowManager _windowManager;
        private readonly Func<Owned<TweetCollection>> _timelineFactory;
        private readonly IDisposable _refreshSubscription;
        private IDisposable _tweetsSubscription;
        private IDisposable _streamingSubscription;
        private string _searchText;
        private string _statusText;
        private string _screenName;
        private bool _showUpdateStatus;
        private DateTime _streamStartTime = DateTime.MinValue;
        private IConnectableObservable<Tweet> _tweetsStream;
        private OutgoingContext _outgoing;

        public ObservableCollection<Owned<TweetCollection>> Timelines { get; private set; }

        public bool ShowUpdateStatus
        {
            get { return _showUpdateStatus; }
            set { this.SetValue("ShowUpdateStatus", value, ref _showUpdateStatus); }
        }

        public bool ShowMentions
        {
            get { return Timelines.Any(t => t.Value.Description.Equals("Mentions")); }
            set
            {
                if (value)
                {
                    var mentions = _client.GetMentions().Merge(_tweetsStream.Where(t => t.Text.Contains(_screenName)));
                    AddTimeline("Mentions", timeline => timeline.Subscribe(mentions));
                }
                else
                {
                    Timelines.Remove(Timelines.Single(t => t.Value.Description.Equals("Mentions")));
                }

                NotifyOfPropertyChange("ShowMentions");
            }
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

        

        public TimelinesViewModel(TwitterClient client, TweetParser tweetParser, IWindowManager windowManager, Func<Owned<TweetCollection>> timelineFactory)
        {
            _client = client;
            _tweetParser = tweetParser;
            _windowManager = windowManager;
            _timelineFactory = timelineFactory;

            Timelines = new ObservableCollection<Owned<TweetCollection>>();
            Timelines.CollectionChanged += (sender, e) =>
            {
                if (e.OldItems != null)
                    e.OldItems.Cast<Owned<TweetCollection>>().ForEach(t => t.Dispose());
            };

            client.GetCredentialVerification()
                .Select(x => "@" + x["screen_name"])
                .Select(name => new { name, tweets = client.GetStreamingStatuses().Publish() })
                .DispatcherSubscribe(x =>
                {
                    _screenName = x.name;
                    _tweetsStream = x.tweets;
                    OnInit(x.name, x.tweets);
                });

            _refreshSubscription = Observable.Interval(TimeSpan.FromSeconds(20))
                .DispatcherSubscribe(_ => Timelines
                                              .Select(x => x.Value)
                                              .SelectMany(x => x)
                                              .ForEach(t => t.NotifyOfPropertyChange("CreatedAt")));
        }

        private void OnInit(string name, IConnectableObservable<Tweet> tweets)
        {
            _screenName = name;
            _tweetsStream = tweets;

            AddTimeline("Home", line => line.Subscribe(_tweetsStream.Where(t => !t.Text.Contains(_screenName))));

            ShowMentions = true;
            _tweetsSubscription = tweets.Connect();
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
            _streamingSubscription.DisposeIfNotNull();
            _tweetsSubscription.DisposeIfNotNull();
            _refreshSubscription.Dispose();
        }

        public void OnStatusTextBoxTextInput(TextCompositionEventArgs e)
        {
            if (e.Text[0] == 27) // esc
                ShowUpdateStatus = false;
        }

        public void OnStatusTextBoxChanged(TextBox sender, TextChangedEventArgs e)
        {
            int length;
            string text = sender.Text;
            _tweetParser.Parse(sender.Text, out length);
            if (text.Contains("\r") && length <= TweetParser.MaxLength)
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

                Timelines
                    .Where(line => line.Value.Tag is string[])
                    .ToArray()
                    .ForEach(t => Timelines.Remove(t));

                var allTerms = SearchText.Split(' ', ',', ';', '|');
                var allParts = SearchText.Split(' ', ',', ';');
                var ob = _client.GetStreamingFilter(allTerms).Publish();

                foreach (string part in allParts)
                {
                    string[] terms = part.Split('|');
                    var sub = ob.Where(t => terms.Any(term => t.Text.Contains(term)));
                    AddTimeline(part, tl =>
                    {
                        tl.Tag = terms;
                        tl.CanClose = true;
                        tl.Subscribe(sub);
                    });
                }

                _streamingSubscription.DisposeIfNotNull();
                _streamingSubscription = ob.Connect();
            }
        }

        public void Stop()
        {
            _streamingSubscription.DisposeIfNotNull();
        }

        private void AddTimeline(string description, Action<TweetCollection> setup)
        {
            var timeline = _timelineFactory();
            var line = timeline.Value;
            line.Description = description;
            line.OnError += ex => _windowManager.ShowDialog(new ErrorViewModel(ex.ToString()));
            line.Closed += (sender, e) => Timelines.Remove(Timelines.Single(t => t.Value == sender));
            setup(line);
            Timelines.Add(timeline);
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
            AddTimeline("@" + message.User, timeline =>
            {
                timeline.CanClose = true;
                timeline.Subscribe(_client.GetPollingUserTimeline(message.User));
            });
        }

        void IHandle<NavigateToTopicMessage>.Handle(NavigateToTopicMessage message)
        {
            AddTimeline("#" + message.Topic, timeline =>
            {
                timeline.CanClose = true;
                timeline.Subscribe(_client.GetPollingSearch(message.Topic));
            });
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