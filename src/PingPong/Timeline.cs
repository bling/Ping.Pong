using System;
using System.Reactive.Subjects;
using PingPong.Models;

namespace PingPong
{
    public class Timeline : TweetCollection, IDisposable, IObservable<Tweet>
    {
        protected const int MaxTweets = 1000;

        private IDisposable _subscription;
        private readonly Subject<Tweet> _subject = new Subject<Tweet>();

        public event Action<Exception> OnError;

        IDisposable IObservable<Tweet>.Subscribe(IObserver<Tweet> observer)
        {
            return _subject.Subscribe(observer);
        }

        public void Subscribe(IObservable<Tweet> tweets)
        {
            _subscription.DisposeIfNotNull();
            _subscription = tweets
                //.Do(t => _subject.OnNext(t))
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
                if ((ulong)tweet.Id > first.Id)
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