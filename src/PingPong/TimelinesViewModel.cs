using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using Autofac.Features.OwnedInstances;
using Caliburn.Micro;
using PingPong.Controls;
using PingPong.Core;
using PingPong.Messages;

namespace PingPong
{
    public class TimelinesViewModel : Screen, IHandle<NavigateToUserMessage>, IHandle<NavigateToTopicMessage>, IHandle<NavigateToConversationMessage>
    {
        private static readonly TimeSpan StreamThrottleRate = TimeSpan.FromSeconds(20);

        private readonly AppInfo _appInfo;
        private readonly TwitterClient _client;
        private readonly IWindowManager _windowManager;
        private readonly Func<Owned<TweetsPanelViewModel>> _timelineFactory;
        private readonly Owned<TweetsPanelViewModel> _homeline;
        private readonly Owned<TweetsPanelViewModel> _mentionline;
        private readonly Owned<TweetsPanelViewModel> _messageline;
        private IDisposable _tweetsSubscription;
        private IDisposable _streamingSubscription;
        private IDisposable _notificationSubscription;
        private string _searchText;
        private bool _streaming;
        private bool _isBusy;
        private DateTime _streamStartTime = DateTime.MinValue;

        public ObservableCollection<Owned<TweetsPanelViewModel>> Timelines { get; private set; }

        public bool ShowHome
        {
            get { return Timelines.Contains(_homeline); }
            set
            {
                if (value)
                    Timelines.Add(_homeline);
                else
                    Timelines.Remove(_homeline);

                NotifyOfPropertyChange(() => ShowHome);
            }
        }

        public bool ShowMentions
        {
            get { return Timelines.Contains(_mentionline); }
            set
            {
                if (value)
                    Timelines.Add(_mentionline);
                else
                    Timelines.Remove(_mentionline);

                NotifyOfPropertyChange(() => ShowMentions);
            }
        }


        public bool ShowMessages
        {
            get { return Timelines.Contains(_messageline); }
            set
            {
                if (value)
                {
                    _messageline.Value.Subscribe(_client.GetPollingDirectMessages());
                    Timelines.Add(_messageline);
                }
                else
                {
                    _messageline.Value.StopSubscription();
                    Timelines.Remove(_messageline);
                }   

                NotifyOfPropertyChange(() => ShowMessages);
            }
        }

        public string SearchText
        {
            get { return _searchText; }
            set { this.SetValue("SearchText", value, ref _searchText); }
        }

        public bool Streaming
        {
            get { return _streaming; }
            set
            {
                this.SetValue("Streaming", value, ref _streaming);
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

        public TimelinesViewModel(AppInfo appInfo, TwitterClient client, IWindowManager windowManager, Func<Owned<TweetsPanelViewModel>> timelineFactory)
        {
            _appInfo = appInfo;
            _client = client;
            _windowManager = windowManager;
            _timelineFactory = timelineFactory;

            _homeline = timelineFactory();
            _homeline.Value.DisplayName = "Home";

            _mentionline = timelineFactory();
            _mentionline.Value.DisplayName = "Mentions";

            _messageline = timelineFactory();
            _messageline.Value.DisplayName = "Messages";

            Timelines = new ObservableCollection<Owned<TweetsPanelViewModel>>();
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            IsBusy = true;

            Observable.FromEventPattern<NotifyCollectionChangedEventArgs>(Timelines, "CollectionChanged")
                .Where(x => x.EventArgs.OldItems != null)
                .SelectMany(x => x.EventArgs.OldItems.Cast<Owned<TweetsPanelViewModel>>())
                .Where(_ => !Timelines.Any(t => t.Value.Tag is string[])) // streaming columns
                .Subscribe(_ => _streamingSubscription.DisposeIfNotNull());

            _client.GetAccountVerification()
                .Do(x => _appInfo.User = x)
                .Select(x => "@" + x.ScreenName)
                .Select(atName => new { atName, stream = _client.GetStreamingStatuses().Publish() })
                .DispatcherSubscribe(x =>
                {
                    _homeline.Value.Subscribe(x.stream.Where(t => !t.Text.Contains(x.atName)));
                    _mentionline.Value.Subscribe(x.stream.Where(t => t.Text.Contains(x.atName)));
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
            _homeline.Dispose();
            _mentionline.Dispose();
            _messageline.Dispose();
        }

        private void StartStreaming()
        {
            if (DateTime.UtcNow - _streamStartTime < StreamThrottleRate)
            {
                _windowManager.ShowDialog(new ErrorViewModel("You are initiating too many connections in a short period of time.  Twitter doesn't like that :("));
                Streaming = false;
            }
            else if (string.IsNullOrEmpty(SearchText))
            {
                _windowManager.ShowDialog(new ErrorViewModel("Search terms are required."));
                Streaming = false;
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
                    AddTimeline(part, tl =>
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

        private void AddTimeline(string description, Action<TweetsPanelViewModel> setup)
        {
            var timeline = _timelineFactory();
            var line = timeline.Value;
            line.DisplayName = description;
            line.Deactivated += (sender, e) =>
            {
                var value = Timelines.Single(t => t.Value == sender);
                Timelines.Remove(value);
                value.Dispose();
            };
            setup(line);
            Timelines.Add(timeline);
        }

        public void MoveLeft(TweetsPanelViewModel source)
        {
            var target = Timelines.FirstOrDefault(t => t.Value == source);
            if (target != null)
            {
                int index = Timelines.IndexOf(target);
                if (index - 1 >= 0)
                {
                    Timelines.RemoveAt(index);
                    Timelines.Insert(index - 1, target);
                }
            }
        }

        public void MoveRight(TweetsPanelViewModel source)
        {
            var target = Timelines.FirstOrDefault(t => t.Value == source);
            if (target != null)
            {
                int index = Timelines.IndexOf(target);
                if (index + 1 < Timelines.Count)
                {
                    Timelines.RemoveAt(index);
                    Timelines.Insert(index + 1, target);
                }
            }
        }

        void IHandle<NavigateToUserMessage>.Handle(NavigateToUserMessage message)
        {
            AddTimeline("@" + message.User, timeline =>
            {
                timeline.CanClose = true;
                timeline.SubscribeToUserTimeline(message.User);
            });
        }

        void IHandle<NavigateToTopicMessage>.Handle(NavigateToTopicMessage message)
        {
            AddTimeline("#" + message.Topic, timeline =>
            {
                timeline.CanClose = true;
                timeline.SubscribeToTopic(message.Topic);
            });
        }

        void IHandle<NavigateToConversationMessage>.Handle(NavigateToConversationMessage message)
        {
            AddTimeline(message.User1 + "/" + message.User2, timeline =>
            {
                timeline.CanClose = true;
                timeline.SubscribeToConversation(message.User1, message.User2);
            });
        }
    }
}