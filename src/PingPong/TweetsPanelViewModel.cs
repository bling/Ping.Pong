using System;
using System.Linq;
using System.Reactive.Linq;
using Caliburn.Micro;
using PingPong.Core;
using PingPong.Models;

namespace PingPong
{
    public class TweetsPanelViewModel : Screen
    {
        private readonly AppInfo _appInfo;
        private readonly TwitterClient _client;
        private readonly IWindowManager _windowManager;
        private bool _isBusy;
        private bool _canClose;
        private bool _canOpenUserInfo;
        private bool _isUserInfoOpen;
        private IDisposable _subscription;
        private string _currentUsername;
        private ExtendedUser _user;

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
                        .DispatcherSubscribe(x =>
                        {
                            User = new ExtendedUser(x);
                            _client.GetRelationship(_appInfo.User.ScreenName, _currentUsername)
                                .DispatcherSubscribe(r =>
                                {
                                    User.Following = r.Source.IsFollowing;
                                    User.FollowsBack = r.Source.IsFollowedBy;
                                });
                        });
                }
            }
        }

        public ExtendedUser User
        {
            get { return _user; }
            private set { this.SetValue("User", value, ref _user); }
        }

        public TweetsPanelViewModel(AppInfo appInfo, TwitterClient client, IWindowManager windowManager)
        {
            _appInfo = appInfo;
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