using System;
using System.Linq;
using System.Reactive.Subjects;
using PingPong.Models;

namespace PingPong.Timelines
{
    public class StreamingTimeline : Timeline
    {
        private readonly IConnectableObservable<Tweet> _observable;

        public string[] FilterTerms { get; set; }

        public StreamingTimeline(IConnectableObservable<Tweet> observable)
        {
            _observable = observable;
        }

        protected override IDisposable StartSubscription()
        {
            return _observable.DispatcherSubscribe(Subscribe, RaiseOnError);
        }

        private void Subscribe(Tweet tweet)
        {
            if (FilterTerms != null)
            {
                if (!FilterTerms.Any(t => tweet.Text.Contains(t)))
                    return;
            }

            AddToFront(tweet);
        }
    }
}