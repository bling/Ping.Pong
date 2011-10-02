using System;
using System.Linq;
using System.Reactive.Linq;
using Caliburn.Micro;
using PingPong.Core;
using PingPong.Models;

namespace PingPong.ViewModels
{
    public class TweetsPanelViewModel : Screen
    {
        private readonly TwitterClient _client;
        private readonly IWindowManager _windowManager;
        private readonly Func<string, UserViewModel> _userViewModelFactory;
        private bool _isBusy;
        private bool _canClose;
        private bool _canOpenInfoBox;
        private object _contextualViewModel;
        private IDisposable _subscription;

        /// <summary>Object to get or set metadata on the collection.</summary>
        public object Tag { get; set; }

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

        public TweetsPanelViewModel(TwitterClient client, IWindowManager windowManager, Func<string, UserViewModel> userViewModelFactory)
        {
            _client = client;
            _windowManager = windowManager;
            _userViewModelFactory = userViewModelFactory;
            Tweets = new TweetCollection();
        }

        public new void TryClose()
        {
            _subscription.DisposeIfNotNull();
            ((IConductor)Parent).DeactivateItem(this, true);
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

        public void SubscribeToConversation(string user1, string user2)
        {
            Enforce.NotNullOrEmpty(user1);
            Enforce.NotNullOrEmpty(user2);
            Observable.Start(() =>
            {
                var results = _client.GetSearch(string.Format("from:{0} to:{1}", user1, user2))
                    .Merge(_client.GetSearch(string.Format("from:{1} to:{0}", user1, user2)))
                    .ToEnumerable()
                    .OrderByDescending(x => x.CreatedAt)
                    .ToArray();

                Subscribe(results.ToObservable());
            });
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
                .Subscribe(x => Tweets.Append(x), RaiseOnError, () => IsBusy = false);
            ((IActivate)this).Activate();
        }

        /// <summary>Stops the current subscription, if there is one.</summary>
        public void StopSubscription()
        {
            _subscription.DisposeIfNotNull();
        }

        private void RaiseOnError(Exception ex)
        {
            _windowManager.ShowDialog(new ErrorViewModel(ex.ToString()));
        }
    }
}