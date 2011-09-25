using System;
using System.Reactive.Linq;
using Caliburn.Micro;
using PingPong.Core;
using PingPong.Models;

namespace PingPong
{
    public class TweetsPanelViewModel : Screen
    {
        private readonly TwitterClient _client;
        private readonly IWindowManager _windowManager;
        private bool _isBusy;
        private bool _canClose;
        private bool _canOpenUserInfo;
        private bool _isUserInfoOpen;
        private IDisposable _subscription;
        private string _currentUsername;
        private User _user;

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

        public bool CanOpenUserInfo
        {
            get { return _canOpenUserInfo; }
            set { this.SetValue("CanOpenUserInfo", value, ref _canOpenUserInfo); }
        }

        public bool IsUserInfoOpen
        {
            get { return _isUserInfoOpen; }
            set
            {
                if (this.SetValue("IsUserInfoOpen", value, ref _isUserInfoOpen) && value)
                {
                    Enforce.NotNullOrEmpty(_currentUsername);
                    _client.GetUserInfo(_currentUsername)
                        .DispatcherSubscribe(x => User = x);
                }
            }
        }

        public User User
        {
            get { return _user; }
            private set { this.SetValue("User", value, ref _user); }
        }

        public TweetsPanelViewModel(TwitterClient client, IWindowManager windowManager)
        {
            _client = client;
            _windowManager = windowManager;
            Tweets = new TweetCollection();
        }

        public new void TryClose()
        {
            _subscription.DisposeIfNotNull();
            ((IDeactivate)this).Deactivate(true);
        }

        public void SubscribeToUserTimeline(string username)
        {
            _currentUsername = username;
            Subscribe(_client.GetPollingUserTimeline(username), _ => CanOpenUserInfo = true);
        }

        public void SubscribeToTopic(string topic)
        {
            Enforce.NotNullOrEmpty(topic);
            Subscribe(_client.GetPollingSearch(topic));
        }

        public void Subscribe(IObservable<Tweet> tweets, Action<Tweet> optionalActionOnSubscribe = null)
        {
            optionalActionOnSubscribe = optionalActionOnSubscribe ?? (_ => { });

            IsBusy = true;
            _subscription.DisposeIfNotNull();
            _subscription = tweets
                .SubscribeOnThreadPool()
                .ObserveOnDispatcher()
                .Do(_ => IsBusy = false)
                .Do(x => optionalActionOnSubscribe(x))
                .Subscribe(x => Tweets.Append(x), RaiseOnError);
            ((IActivate)this).Activate();
        }

        private void RaiseOnError(Exception ex)
        {
            _windowManager.ShowDialog(new ErrorViewModel(ex.ToString()));
        }
    }
}