using System;
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
            Enforce.NotNull(_appInfo.User);
            Subscribe(_client.GetPollingUserTimeline(username));
        }

        public void SubscribeToTopic(string topic)
        {
            Enforce.NotNullOrEmpty(topic);
            Subscribe(_client.GetPollingSearch(topic));
        }

        public void Subscribe(IObservable<Tweet> tweets)
        {
            IsBusy = true;
            _subscription.DisposeIfNotNull();
            _subscription = tweets
                .SubscribeOnThreadPool()
                .ObserveOnDispatcher()
                .Do(_ => IsBusy = false)
                .Subscribe(x => Tweets.Append(x), RaiseOnError);
            ((IActivate)this).Activate();
        }

        private void RaiseOnError(Exception ex)
        {
            _windowManager.ShowDialog(new ErrorViewModel(ex.ToString()));
        }
    }
}