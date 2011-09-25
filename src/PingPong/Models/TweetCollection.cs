using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Caliburn.Micro;

namespace PingPong.Models
{
    public class TweetCollection : ObservableCollection<Tweet>, IDisposable, IObservable<Tweet>, IClose, INotifyPropertyChangedEx
    {
        protected const int MaxTweets = 1000;

        private IDisposable _subscription;
        private readonly Subject<Tweet> _subject = new Subject<Tweet>();
        private bool _canClose;
        private bool _isNotifying;
        private bool _isBusy;

        public event Action<Exception> OnError;
        public event EventHandler Closed;

        /// <summary>Object to get or set metadata on the collection.</summary>
        public object Tag { get; set; }

        /// <summary>Gets or sets the header text to display.</summary>
        public string Description { get; set; }

        public bool IsBusy
        {
            get { return _isBusy; }
            set { this.SetValue("IsBusy", value, ref _isBusy); }
        }

        public bool CanClose
        {
            get { return _canClose; }
            set { this.SetValue("CanClose", value, ref _canClose); }
        }

        IDisposable IObservable<Tweet>.Subscribe(IObserver<Tweet> observer)
        {
            return _subject.Subscribe(observer);
        }

        public void Subscribe(IObservable<Tweet> tweets)
        {
            IsBusy = true;
            _subscription.DisposeIfNotNull();
            _subscription = tweets
                .Do(t => _subject.OnNext(t))
                .SubscribeOnThreadPool()
                .ObserveOnDispatcher()
                .Do(_ => IsBusy = false)
                .Subscribe(Append, RaiseOnError);
        }

        public void Dispose()
        {
            OnError = null;
            Closed = null;
            Clear();
            _subject.Dispose();
            _subscription.DisposeIfNotNull();
            GC.SuppressFinalize(this);
        }

        private void Append(Tweet tweet)
        {
            while (Count > MaxTweets)
                RemoveAt(Count - 1);

            Tweet first = Count > 0 ? this[0] : null;
            if (first != null)
            {
                if (tweet.Id > first.Id)
                {
                    Insert(0, tweet);
                    return;
                }
            }
            Add(tweet);
        }

        private void RaiseOnError(Exception ex)
        {
            var e = OnError;
            if (e != null) e(ex);
        }

        public void TryClose()
        {
            var e = Closed;
            if (e != null) e(this, EventArgs.Empty);
        }

        void INotifyPropertyChangedEx.NotifyOfPropertyChange(string propertyName)
        {
            base.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        void INotifyPropertyChangedEx.Refresh()
        {
        }

        bool INotifyPropertyChangedEx.IsNotifying
        {
            get { return _isNotifying; }
            set { _isNotifying = true; }
        }
    }
}