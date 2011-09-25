using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
        private readonly Owned<TweetCollection> _homeline;
        private readonly Owned<TweetCollection> _mentionline;
        private IDisposable _tweetsSubscription;
        private IDisposable _streamingSubscription;
        private string _searchText;
        private string _screenName;
        private bool _streaming;
        private bool _isBusy;
        private DateTime _streamStartTime = DateTime.MinValue;
        private IConnectableObservable<Tweet> _tweetsStream;

        public ObservableCollection<Owned<TweetCollection>> Timelines { get; private set; }

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

        public TimelinesViewModel(TwitterClient client, IWindowManager windowManager, Func<Owned<TweetCollection>> timelineFactory)
        {
            _client = client;
            _windowManager = windowManager;
            _timelineFactory = timelineFactory;

            _homeline = timelineFactory();
            _homeline.Value.Description = "Home";

            _mentionline = timelineFactory();
            _mentionline.Value.Description = "Mentions";

            IsBusy = true;

            Timelines = new ObservableCollection<Owned<TweetCollection>>();
            Timelines.CollectionChanged += OnTimelinesCollectionChanged;

            _refreshSubscription = Observable.Interval(TimeSpan.FromSeconds(20))
                .DispatcherSubscribe(_ => Timelines
                                              .Select(x => x.Value)
                                              .SelectMany(x => x)
                                              .ForEach(t => t.NotifyOfPropertyChange("CreatedAt")));
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            _client.GetAccountVerification()
                .Select(x => "@" + x.ScreenName)
                .Do(x => _screenName = x)
                .Do(_ => _tweetsStream = _client.GetStreamingStatuses().Publish())
                .DispatcherSubscribe(_ =>
                {
                    _homeline.Value.Subscribe(_tweetsStream);
                    _mentionline.Value.Subscribe(_tweetsStream.Where(t => t.Text.Contains(_screenName)));
                    _tweetsSubscription = _tweetsStream.Connect();

                    ShowHome = true;
                    ShowMentions = true;
                    IsBusy = false;
                });
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
            _streamingSubscription.DisposeIfNotNull();
            _tweetsSubscription.DisposeIfNotNull();
            _homeline.Dispose();
            _mentionline.Dispose();
            _refreshSubscription.Dispose();
        }

        private void OnTimelinesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                e.OldItems.Cast<Owned<TweetCollection>>()
                    .Except(new[] { _homeline, _mentionline })
                    .ForEach(t => t.Dispose());

            if (!Timelines.Any(t => t.Value.Tag is string[])) // streaming columns
                _streamingSubscription.DisposeIfNotNull();
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

        public void MoveLeft(TweetCollection source)
        {
            Timelines.CollectionChanged -= OnTimelinesCollectionChanged;
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
            Timelines.CollectionChanged += OnTimelinesCollectionChanged;
        }

        public void MoveRight(TweetCollection source)
        {
            Timelines.CollectionChanged -= OnTimelinesCollectionChanged;
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
            Timelines.CollectionChanged += OnTimelinesCollectionChanged;
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
}