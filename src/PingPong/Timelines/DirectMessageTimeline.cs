using System;
using System.Reactive.Linq;

namespace PingPong.Timelines
{
    public class DirectMessageTimeline : Timeline
    {
        private string _sinceId;

        protected override IDisposable StartSubscription()
        {
            return CreateTimerObservable()
                .SelectMany(_ => Client.GetDirectMessages(_sinceId))
                .Do(tweet => _sinceId = tweet.Id)
                .DispatcherSubscribe(AddToEnd);
        }
    }
}