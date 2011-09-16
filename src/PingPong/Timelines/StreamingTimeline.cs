namespace PingPong.Timelines
{
    using System;
    using System.Linq;
    using System.Reactive.Subjects;
    using PingPong.Models;

    public class StreamingTimeline : Timeline
    {
        private IDisposable _subscription;

        public string[] FilterTerms { get; set; }

        protected override IDisposable StartSubscription()
        {
            if (_subscription == null) throw new InvalidOperationException();

            return _subscription;
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

        public void Start(IConnectableObservable<Tweet> observable)
        {
            _subscription = observable.DispatcherSubscribe(Subscribe);
            Start();
        }
    }
}