using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace PingPong.Models
{
    public class TweetCollection : ObservableCollection<Tweet>, IDisposable, IObservable<Tweet>
    {
        protected const int MaxTweets = 1000;

        private IDisposable _subscription;
        private readonly Subject<Tweet> _subject = new Subject<Tweet>();

        public event Action<Exception> OnError;

        /// <summary>Object to get or set metadata on the collection.</summary>
        public object Tag { get; set; }

        IDisposable IObservable<Tweet>.Subscribe(IObserver<Tweet> observer)
        {
            return _subject.Subscribe(observer);
        }

        public void Subscribe(IObservable<Tweet> tweets)
        {
            _subscription.DisposeIfNotNull();
            _subscription = tweets
                .Do(t => _subject.OnNext(t))
                .DispatcherSubscribe(Append, RaiseOnError);
        }

        public void Dispose()
        {
            OnError = null;
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
    }

    public enum StatusType
    {
        Home,
        Mentions,
    }
}