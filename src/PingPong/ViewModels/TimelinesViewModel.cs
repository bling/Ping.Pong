using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Caliburn.Micro;
using PingPong.Core;
using PingPong.Models;

namespace PingPong.ViewModels
{
    public class TimelinesViewModel : Conductor<TweetsPanelViewModel>.Collection.AllActive, ITimelineNavigator, IDisposable
    {
        private static readonly TimeSpan StreamThrottleRate = TimeSpan.FromSeconds(20);

        private readonly AppInfo _appInfo;
        private readonly TwitterClient _client;
        private readonly IWindowManager _windowManager;
        private readonly Func<TweetsPanelViewModel> _timelineFactory;
        private readonly TweetsPanelViewModel _homeline;
        private readonly TweetsPanelViewModel _mentionline;
        private readonly TweetsPanelViewModel _messageline;
        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();
        private IDisposable _streamingSubscription;
        private RateLimit _rateLimit;
        private List _currentList;
        private string _searchText;
        private bool _isBusy;
        private DateTime _streamStartTime = DateTime.MinValue;

        public bool ShowHome
        {
            get { return Items.Contains(_homeline); }
            set
            {
                if (value)
                    ActivateItem(_homeline);
                else
                    DeactivateItem(_homeline, true);

                NotifyOfPropertyChange(() => ShowHome);
            }
        }

        public bool ShowMentions
        {
            get { return Items.Contains(_mentionline); }
            set
            {
                if (value)
                    ActivateItem(_mentionline);
                else
                    DeactivateItem(_mentionline, true);

                NotifyOfPropertyChange(() => ShowMentions);
            }
        }

        public bool ShowMessages
        {
            get { return Items.Contains(_messageline); }
            set
            {
                if (value)
                {
                    _messageline.Subscribe(_client.GetPollingDirectMessages());
                    ActivateItem(_messageline);
                }
                else
                {
                    DeactivateItem(_messageline, true);
                }

                NotifyOfPropertyChange(() => ShowMessages);
            }
        }

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                this.SetValue("SearchText", value, ref _searchText);
                AppSettings.LastSearchTerms = value;
            }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            set { this.SetValue("IsBusy", value, ref _isBusy); }
        }

        public RateLimit RateLimit
        {
            get { return _rateLimit; }
            private set { this.SetValue("RateLimit", value, ref _rateLimit); }
        }

        public ObservableCollection<List> Lists { get; private set; }

        public List CurrentList
        {
            get { return _currentList; }
            set
            {
                this.SetValue("CurrentList", value, ref _currentList);
                if (value != null)
                {
                    var obs = _client.GetPollingListStatuses(value.Id);
                    ActivateTimeline(value.FullName, tl =>
                    {
                        tl.CanClose = true;
                        tl.Subscribe(obs);
                    });
                    CurrentList = null;
                }
            }
        }

        public TimelinesViewModel(AppInfo appInfo, TwitterClient client, IWindowManager windowManager, Func<TweetsPanelViewModel> timelineFactory)
        {
            Lists = new ObservableCollection<List>();

            _appInfo = appInfo;
            _client = client;
            _windowManager = windowManager;
            _timelineFactory = timelineFactory;

            _homeline = timelineFactory();
            _homeline.DisplayName = "Home";

            _mentionline = timelineFactory();
            _mentionline.DisplayName = "Mentions";

            _messageline = timelineFactory();
            _messageline.DisplayName = "Messages";

            IsBusy = true;

            Observable.FromEventPattern<NotifyCollectionChangedEventArgs>(Items, "CollectionChanged")
                .Where(x => x.EventArgs.OldItems != null)
                .SelectMany(x => x.EventArgs.OldItems.Cast<TweetsPanelViewModel>())
                .Where(_ => !Items.Any(t => t.Tag is string[])) // streaming columns
                .Subscribe(_ => _streamingSubscription.DisposeIfNotNull());

            string user = '@' + _appInfo.User.ScreenName;
            var stream = _client.GetStreamingStatuses().Publish();
            _homeline.Subscribe(stream.Where(t => !t.Text.Contains(user)));
            _mentionline.Subscribe(stream.Where(t => t.Text.Contains(user)));
            _subscriptions.Add(stream.Connect());

            ShowHome = true;
            ShowMentions = true;
            ShowMessages = false;
            IsBusy = false;

            _subscriptions.Add(_client.GetPollingRateLimitStatus().DispatcherSubscribe(rl => RateLimit = rl));
            _subscriptions.Add(_client.GetLists(_appInfo.User.ScreenName).DispatcherSubscribe(x => Lists.Add(x)));

            if (!string.IsNullOrEmpty(AppSettings.LastSearchTerms))
            {
                SearchText = AppSettings.LastSearchTerms;
                StartStreaming(SearchText);
            }
        }

        public void Dispose()
        {
            _streamingSubscription.DisposeIfNotNull();
            _subscriptions.Dispose();
        }

        public void StartStreaming(string query)
        {
            if (DateTime.UtcNow - _streamStartTime < StreamThrottleRate)
            {
                _windowManager.ShowDialog(new ErrorViewModel("You are initiating too many connections in a short period of time.  Twitter doesn't like that :("));
            }
            else if (string.IsNullOrEmpty(query))
            {
                _windowManager.ShowDialog(new ErrorViewModel("Search terms are required."));
            }
            else
            {
                _streamStartTime = DateTime.UtcNow;

                Items
                    .Where(line => line.Tag is string[])
                    .ToArray()
                    .ForEach(t => DeactivateItem(t, true));

                var allTerms = query.Split(' ', ',', ';', '|');
                var allParts = query.Split(' ', ',', ';');
                var ob = _client.GetStreamingFilter(allTerms)
                    .Retry()
                    .Publish();

                foreach (string part in allParts)
                {
                    string[] terms = part.Split('|');
                    var combo = _client.GetSearch(part).Cast<ITweetItem>().Concat(ob);
                    ActivateTimeline(part, tl =>
                    {
                        tl.Tag = terms;
                        tl.CanClose = true;
                        tl.Subscribe(combo.Where(t => terms.Any(term => t.Text.Contains(term))));
                    });
                }

                _streamingSubscription.DisposeIfNotNull();
                _streamingSubscription = ob.Connect();
            }
        }

        private void ActivateTimeline(string description, Action<TweetsPanelViewModel> setup)
        {
            var line = _timelineFactory();
            line.DisplayName = description;
            setup(line);
            ActivateItem(line);
        }

        public void Search(string text)
        {
            ActivateTimeline(text, line =>
            {
                line.CanClose = true;
                line.Subscribe(_client.GetPollingSearch(text));
            });
        }

        public void MoveLeft(TweetsPanelViewModel source)
        {
            var target = Items.FirstOrDefault(t => t == source);
            if (target != null)
            {
                int index = Items.IndexOf(target);
                if (index - 1 >= 0)
                {
                    Items.RemoveAt(index);
                    Items.Insert(index - 1, target);
                }
            }
        }

        public void MoveRight(TweetsPanelViewModel source)
        {
            var target = Items.FirstOrDefault(t => t == source);
            if (target != null)
            {
                int index = Items.IndexOf(target);
                if (index + 1 < Items.Count)
                {
                    Items.RemoveAt(index);
                    Items.Insert(index + 1, target);
                }
            }
        }

        public void NavigateToTopicMessage(string topic)
        {
            ActivateTimeline(topic, timeline =>
            {
                timeline.CanClose = true;
                timeline.SubscribeToTopic(topic);
            });
        }

        public void NavigateToUserTimeline(string screenName)
        {
            screenName = screenName.Trim(TweetParser.PunctuationChars);
            ActivateTimeline(screenName, timeline =>
            {
                timeline.CanClose = true;
                timeline.SubscribeToUserTimeline(screenName.Trim('@'));
            });
        }

        public void NavigateToConversationTimeline(ITweetItem item)
        {
            if (item is Tweet)
            {
                var tweet = (Tweet)item;
                ActivateTimeline("Conversation", timeline =>
                {
                    timeline.CanClose = true;
                    timeline.SubscribeToConversation(tweet);
                });
            }
        }
    }
}