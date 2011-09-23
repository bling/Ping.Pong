using System;
using System.Reactive.Linq;
using PingPong.Models;

namespace PingPong.Core
{
    public static class TwitterSubscriptions
    {
        private const int DefaultPollSeconds = 60;

        private static IObservable<long> CreateTimerObservable()
        {
            return Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(DefaultPollSeconds));
        }

        public static IObservable<Tweet> GetPollingDirectMessages(this TwitterClient client)
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

        public static IObservable<Tweet> GetPollingStatuses(this TwitterClient client)
        {
            return client.GetHomeTimeline();
                //.Merge(client.GetMentions())
                //.Merge(client.GetStreamingHomeline());
        }

        public static IObservable<Tweet> GetPollingUserTimeline(this TwitterClient client, string screenName)
        {
            return Observable.Create<Tweet>(obs =>
            {
                ulong? sinceId = null;
                return CreateTimerObservable()
                    .SelectMany(_ => client.GetUserTimeline(screenName, sinceId))
                    .Do(tweet => sinceId = tweet.Id)
                    .Subscribe(obs.OnNext);
            });
        }

        public static IObservable<Tweet> GetPollingSearch(this TwitterClient client, string query)
        {
            Enforce.NotNullOrEmpty(query);

            return Observable.Create<Tweet>(obs =>
            {
                ulong? sinceId = null;
                return CreateTimerObservable()
                    .SelectMany(_ => client.GetSearch(query))
                    .Do(tweet => sinceId = tweet.Id)
                    .Subscribe(obs.OnNext);
            });
        }
    }
}