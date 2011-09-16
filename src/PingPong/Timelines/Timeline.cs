using System;
using System.Linq;
using System.Reactive.Linq;
using PingPong.Models;

namespace PingPong.Timelines
{
    using System.Collections.Generic;

    public abstract class Timeline : TweetCollection, IDisposable
    {
        protected const int MaxTweets = 1000;
        protected const int DefaultPollSeconds = 60;

        private IDisposable _subscription;
        private readonly IDictionary<string, Tweet> _tweets = new Dictionary<string, Tweet>();

        public TwitterClient Client { get; set; }

        protected void AddToEnd(Tweet tweet)
        {
            if (Count > MaxTweets)
            {
                _tweets.Remove(this[0].Id);
                RemoveAt(0);
            }

            if (_tweets.ContainsKey(tweet.Id))
                return;

            _tweets[tweet.Id] = tweet;
            Add(tweet);
        }

        protected void AddToFront(Tweet tweet)
        {
            if (Count > MaxTweets)
            {
                _tweets.Remove(this.Last().Id);
                RemoveAt(Count - 1);
            }

            if (_tweets.ContainsKey(tweet.Id))
                return;

            _tweets[tweet.Id] = tweet;
            Insert(0, tweet);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Clear();
            _subscription.DisposeIfNotNull();
        }

        public void Start()
        {
            _subscription = StartSubscription();
        }

        protected abstract IDisposable StartSubscription();

        protected IObservable<long> CreateTimerObservable()
        {
            return Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(DefaultPollSeconds));
        }
    }
}