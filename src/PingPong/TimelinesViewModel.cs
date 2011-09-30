using System;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using Caliburn.Micro;
using PingPong.Controls;
using PingPong.Core;
using PingPong.Messages;

namespace PingPong
{
    public class TimelinesViewModel : Conductor<TweetsPanelViewModel>.Collection.AllActive, IHandle<NavigateToUserMessage>, IHandle<NavigateToTopicMessage>, IHandle<NavigateToConversationMessage>
    {
        private static readonly TimeSpan StreamThrottleRate = TimeSpan.FromSeconds(20);

        private readonly AppInfo _appInfo;
        private readonly TwitterClient _client;
        private readonly IWindowManager _windowManager;
        private readonly Func<TweetsPanelViewModel> _timelineFactory;
        private readonly TweetsPanelViewModel _homeline;
        private readonly TweetsPanelViewModel _mentionline;
        private readonly TweetsPanelViewModel _messageline;
        private IDisposable _tweetsSubscription;
        private IDisposable _streamingSubscription;
        private IDisposable _notificationSubscription;
        private string _searchText;
        private bool isStreaming;
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
                    _messageline.StopSubscription();
                    DeactivateItem(_messageline, true);
                }   

                NotifyOfPropertyChange(() => ShowMessages);
            }
        }

        public string SearchText
        {
            get { return _searchText; }
            set { this.SetValue("SearchText", value, ref _searchText); }
        }

        public bool IsStreaming
        {
            get { return this.isStreaming; }
            set
            {
                this.SetValue("IsStreaming", value, ref this.isStreaming);
                if (value)
                    StartStreaming();
                else
                    _streamingSubscription.DisposeIfNotNull();
            }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            set { this.SetValue("IsBusy", value, ref _isBusy); }
        }

        public TimelinesViewModel(AppInfo appInfo, TwitterClient client, IWindowManager windowManager, Func<TweetsPanelViewModel> timelineFactory)
        {
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
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            IsBusy = true;

            Observable.FromEventPattern<NotifyCollectionChangedEventArgs>(Items, "CollectionChanged")
                .Where(x => x.EventArgs.OldItems != null)
                .SelectMany(x => x.EventArgs.OldItems.Cast<TweetsPanelViewModel>())
                .Where(_ => !Items.Any(t => t.Tag is string[])) // streaming columns
                .Subscribe(_ => _streamingSubscription.DisposeIfNotNull());

            _client.GetAccountVerification()
                .Do(x => _appInfo.User = x)
                .Select(x => "@" + x.ScreenName)
                .Select(atName => new { atName, stream = _client.GetStreamingStatuses().Publish() })
                .DispatcherSubscribe(x =>
                {
                    _homeline.Subscribe(x.stream.Where(t => !t.Text.Contains(x.atName)));
                    _mentionline.Subscribe(x.stream.Where(t => t.Text.Contains(x.atName)));
                    _tweetsSubscription = x.stream.Connect();

                    ShowHome = true;
                    ShowMentions = true;
                    ShowMessages = false;
                    IsBusy = false;

                    _notificationSubscription = _client
                        .Sample(TimeSpan.FromSeconds(6))
                        .Where(t => (DateTime.Now - t.CreatedAt) < TimeSpan.FromSeconds(5))
                        .DispatcherSubscribe(t =>
                                             new NotificationWindow
                                             {
                                                 Width = 300,
                                                 Height = 80,
                                                 Content = new NotificationControl { DataContext = t }
                                             }.Show(5000));
                });
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
            _streamingSubscription.DisposeIfNotNull();
            _tweetsSubscription.DisposeIfNotNull();
            _notificationSubscription.DisposeIfNotNull();
            DeactivateItem(_homeline, true);
            DeactivateItem(_mentionline, true);
            DeactivateItem(_messageline, true);
        }

        private void StartStreaming()
        {
            if (DateTime.UtcNow - _streamStartTime < StreamThrottleRate)
            {
                _windowManager.ShowDialog(new ErrorViewModel("You are initiating too many connections in a short period of time.  Twitter doesn't like that :("));
                IsStreaming = false;
            }
            else if (string.IsNullOrEmpty(SearchText))
            {
                _windowManager.ShowDialog(new ErrorViewModel("Search terms are required."));
                IsStreaming = false;
            }
            else
            {
                _streamStartTime = DateTime.UtcNow;

                Items
                    .Where(line => line.Tag is string[])
                    .ToArray()
                    .ForEach(t => DeactivateItem(t, true));

                var allTerms = SearchText.Split(' ', ',', ';', '|');
                var allParts = SearchText.Split(' ', ',', ';');
                var ob = _client.GetStreamingFilter(allTerms).Publish();

                foreach (string part in allParts)
                {
                    string[] terms = part.Split('|');
                    ActivateTimeline(part, tl =>
                    {
                        tl.Tag = terms;
                        tl.CanClose = true;
                        tl.Subscribe(ob.Where(t => terms.Any(term => t.Text.Contains(term))));
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

        void IHandle<NavigateToUserMessage>.Handle(NavigateToUserMessage message)
        {
            ActivateTimeline("@" + message.User, timeline =>
            {
                timeline.CanClose = true;
                timeline.SubscribeToUserTimeline(message.User);
            });
        }

        void IHandle<NavigateToTopicMessage>.Handle(NavigateToTopicMessage message)
        {
            ActivateTimeline("#" + message.Topic, timeline =>
            {
                timeline.CanClose = true;
                timeline.SubscribeToTopic(message.Topic);
            });
        }

        void IHandle<NavigateToConversationMessage>.Handle(NavigateToConversationMessage message)
        {
            ActivateTimeline(message.User1 + "/" + message.User2, timeline =>
            {
                timeline.CanClose = true;
                timeline.SubscribeToConversation(message.User1, message.User2);
            });
        }
    }
}