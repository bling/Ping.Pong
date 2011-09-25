using System;
using Caliburn.Micro;
using PingPong.Models;

namespace PingPong
{
    public class TweetsPanelViewModel : Screen
    {
        private readonly IWindowManager _windowManager;
        private bool _isBusy;
        private bool _canClose;
        private IDisposable _subscription;

        public bool IsBusy
        {
            get { return _isBusy; }
            set { this.SetValue("IsBusy", value, ref _isBusy); }
        }

        /// <summary>Object to get or set metadata on the collection.</summary>
        public object Tag { get; set; }

        public TweetCollection Tweets { get; private set; }

        public new bool CanClose
        {
            get { return _canClose; }
            set { this.SetValue("CanClose", value, ref _canClose); }
        }

        public TweetsPanelViewModel(IWindowManager windowManager)
        {
            _windowManager = windowManager;
            Tweets = new TweetCollection();
        }

        public new void TryClose()
        {
            _subscription.DisposeIfNotNull();
            ((IDeactivate)this).Deactivate(true);
        }

        public void Subscribe(IObservable<Tweet> tweets)
        {
            _subscription.DisposeIfNotNull();
            _subscription = tweets.DispatcherSubscribe(x => Tweets.Append(x), RaiseOnError);
            ((IActivate)this).Activate();
        }

        private void RaiseOnError(Exception ex)
        {
            _windowManager.ShowDialog(new ErrorViewModel(ex.ToString()));
        }
    }
}