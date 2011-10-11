using System;
using System.Reactive.Linq;
using Caliburn.Micro;
using PingPong.Core;
using PingPong.Models;

namespace PingPong.ViewModels
{
    public class TweetsPanelViewModel : Screen
    {
        private readonly TwitterClient _client;
        private readonly Func<string, UserViewModel> _userViewModelFactory;
        private bool _isBusy;
        private bool _canClose;
        private bool _canOpenInfoBox;
        private object _contextualViewModel;
        private IDisposable _subscription;

        /// <summary>Object to get or set metadata on the collection.</summary>
        public object Tag { get; set; }

        public AppInfo AppInfo { get; private set; }

        public TweetCollection Tweets { get; private set; }

        public bool IsBusy
        {
            get { return _isBusy; }
            set { this.SetValue("IsBusy", value, ref _isBusy); }
        }

        public new bool CanClose
        {
            get { return _canClose; }
            set { this.SetValue("CanClose", value, ref _canClose); }
        }

        public bool CanOpenInfoBox
        {
            get { return _canOpenInfoBox; }
            set { this.SetValue("CanOpenInfoBox", value, ref _canOpenInfoBox); }
        }

        public object ContextualViewModel
        {
            get { return _contextualViewModel; }
            private set { this.SetValue("ContextualViewModel", value, ref _contextualViewModel); }
        }

        public TweetsPanelViewModel(AppInfo appInfo, TwitterClient client, Func<string, UserViewModel> userViewModelFactory)
        {
            AppInfo = appInfo;
            Tweets = new TweetCollection();
            _client = client;
            _userViewModelFactory = userViewModelFactory;
        }

        public void SubscribeToUserTimeline(string username)
        {
            Enforce.NotNullOrEmpty(username);
            ContextualViewModel = _userViewModelFactory(username);
            Subscribe(_client.GetPollingUserTimeline(username), _ => CanOpenInfoBox = true);
        }

        public void SubscribeToTopic(string topic)
        {
            Enforce.NotNullOrEmpty(topic);
            Subscribe(_client.GetPollingSearch(topic));
        }

        public void SubscribeToConversation(Tweet sourceTweet)
        {
            Enforce.NotNull(sourceTweet);
            Subscribe(_client.GetConversation(sourceTweet));
        }

        public void Subscribe(IObservable<ITweetItem> items, Action<ITweetItem> optionalActionOnSubscribe = null)
        {
            optionalActionOnSubscribe = optionalActionOnSubscribe ?? (_ => { });

            IsBusy = true;
            _subscription.DisposeIfNotNull();
            _subscription = items
                .SubscribeOnThreadPool()
                .ObserveOnDispatcher()
                .Do(_ => IsBusy = false)
                .Do(x => optionalActionOnSubscribe(x))
                .Subscribe(x => Tweets.Append(x), () => IsBusy = false);

            ((IActivate)this).Activate();
        }

        protected override void OnDeactivate(bool close)
        {
            _subscription.DisposeIfNotNull();
        }
    }
}