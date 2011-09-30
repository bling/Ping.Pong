using System;
using System.Json;
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

        public static IObservable<Tweet> GetStreamingStatuses(this TwitterClient client)
        {
            return client.GetHomeTimeline()
                .Merge(client.GetMentions())
                .Concat(client.GetStreamingHomeline())
                .Retry();
        }

        public static IObservable<DirectMessage> GetPollingDirectMessages(this TwitterClient client)
        {
            return client.GetPolling((x, sinceId) => x.GetDirectMessages(sinceId));
        }

        public static IObservable<Tweet> GetPollingUserTimeline(this TwitterClient client, string screenName)
        {
            Enforce.NotNullOrEmpty(screenName);
            return client.GetPolling((x, sinceId) => x.GetUserTimeline(screenName, sinceId));
        }

        public static IObservable<SearchResult> GetPollingSearch(this TwitterClient client, string query)
        {
            Enforce.NotNullOrEmpty(query);
            return client.GetPolling((x, sinceId) => x.GetSearch(query, sinceId));
        }

        private static IObservable<T> GetPolling<T>(this TwitterClient client, Func<TwitterClient, string, IObservable<T>> selector) where T : ITweetItem
        {
            return Observable.Create<T>(obs =>
            {
                string sinceId = null;
                return CreateTimerObservable()
                    .SelectMany(_ => selector(client, sinceId))
                    .Do(tweet => sinceId = tweet.Id)
                    .Subscribe(obs.OnNext);
            });
        }

        public static IObservable<Tweet> SelectTweets(this IObservable<JsonValue> observable, IObserver<ITweetItem> observer)
        {
            return observable.SelectWith(observer, JsonHelper.ToTweet);
        }

        public static IObservable<SearchResult> SelectSearchResults(this IObservable<JsonValue> observable, IObserver<ITweetItem> observer)
        {
            return observable.SelectWith(observer, JsonHelper.ToSearchResult);
        }

        public static IObservable<DirectMessage> SelectDirectMessages(this IObservable<JsonValue> observable, IObserver<ITweetItem> observer)
        {
            return observable.SelectWith(observer, JsonHelper.ToDirectMessage);
        }

        private static IObservable<T> SelectWith<T>(this IObservable<JsonValue> observable, IObserver<ITweetItem> observer, Func<JsonObject, T> selector) where T : class, ITweetItem
        {
            return observable
                .Cast<JsonObject>()
                .Select(selector)
                .Where(x => x != null)
                .Do(observer.OnNext);
        }
    }
}