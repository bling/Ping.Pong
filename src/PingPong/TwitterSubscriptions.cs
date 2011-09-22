using System;
using System.Reactive.Linq;
using PingPong.Models;

namespace PingPong
{
    public static class TwitterSubscriptions
    {
        private const int DefaultPollSeconds = 60;

        private static IObservable<long> CreateTimerObservable()
        {
            return Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(DefaultPollSeconds));
        }

        public static IObservable<Tweet> GetDirectMessages(this TwitterClient client)
        {
            return Observable.Create<Tweet>(obs =>
            {
                ulong? sinceId = null;
                return CreateTimerObservable()
                    .SelectMany(_ => client.GetDirectMessages(sinceId))
                    .Do(tweet => sinceId = tweet.Id)
                    .Subscribe(obs.OnNext);
            });
        }

        public static IObservable<Tweet> GetStatuses(this TwitterClient client, StatusType statusType)
        {
            if (statusType == StatusType.Home)
                return client.GetHomeTimeline().Concat(client.GetStreamingHomeline());

            if (statusType == StatusType.Mentions)
            {
                return Observable.Create<Tweet>(obs =>
                {
                    ulong? sinceId = null;
                    return CreateTimerObservable()
                        .SelectMany(_ => client.GetMentions(sinceId))
                        .Do(tweet => sinceId = tweet.Id)
                        .Subscribe(obs.OnNext);
                });
            }

            throw new NotSupportedException(statusType.ToString());
        }
    }
}