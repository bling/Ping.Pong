using System;
using System.Reactive.Linq;

namespace PingPong.Timelines
{
    public class StatusTimeline : Timeline
    {
        private readonly StatusType _statusType;
        private string _sinceId;

        public StatusTimeline(StatusType statusType)
        {
            _statusType = statusType;
        }

        protected override IDisposable StartSubscription()
        {
            if (_statusType == StatusType.Home)
            {
                return Client.GetHomeTimeline()
                    .ObserveOnDispatcher()
                    .Do(AddToEnd)
                    .Concat(Client.GetStreamingHomeline())
                    .DispatcherSubscribe(AddToFront);
            }

            if (_statusType == StatusType.Mentions)
                return CreateTimerObservable()
                    .SelectMany(_ => Client.GetMentions(_sinceId))
                    .Do(tweet => _sinceId = tweet.Id)
                    .DispatcherSubscribe(AddToEnd);

            throw new NotSupportedException(_statusType.ToString());
        }
    }
}