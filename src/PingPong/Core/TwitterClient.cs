using System;
using System.Collections.Generic;
using System.Json;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using PingPong.Models;
using PingPong.OAuth;

namespace PingPong.Core
{
    public class TwitterClient : IObservable<ITweetItem>, IDisposable
    {
        private const int RequestCount = 200;
        private const string ApiAuthority = "https://api.twitter.com";
        private const string SearchAuthority = "https://search.twitter.com";
        private const string StreamingAuthority = "https://stream.twitter.com";
        private const string UserStreamingAuthority = "https://userstream.twitter.com";

        private readonly Subject<ITweetItem> _subject = new Subject<ITweetItem>();

        public void Dispose()
        {
            _subject.Dispose();
        }

        private OAuthClient CreateClient()
        {
            if (!AppSettings.HasAuthToken)
                throw new InvalidOperationException("The current application doesn't have an OAuth token.");

            return new OAuthClient(AppBootstrapper.ConsumerKey, AppBootstrapper.ConsumerSecret, AppSettings.UserOAuthToken, AppSettings.UserOAuthTokenSecret);
        }

        public IObservable<WebResponse> UpdateStatus(string text, string inReplyToStatusId = null)
        {
            Enforce.NotNullOrEmpty(text);
            var client = CreateClient();
            client.Parameters["status"] = text;
            client.Parameters["wrap_links"] = "1";
            if (inReplyToStatusId != null)
                client.Parameters["in_reply_to_status_id"] = inReplyToStatusId;

            client.Url = ApiAuthority + "/1.1/statuses/update.json";
            return client.Post();
        }

        public IObservable<WebResponse> Retweet(string statusId)
        {
            var client = CreateClient();
            client.Url = ApiAuthority + string.Format("/1.1/statuses/retweet/{0}.json", statusId);
            return client.Post();
        }

        public IObservable<WebResponse> DirectMessage(string username, string text)
        {
            Enforce.NotNullOrEmpty(username);
            Enforce.NotNullOrEmpty(text);

            var client = CreateClient();
            client.Url = ApiAuthority + "/1.1/direct_messages/new.json";
            client.Parameters.Add("screen_name", username);
            client.Parameters.Add("text", text);
            return client.Post();
        }

        public IObservable<WebResponse> Follow(string screenName)
        {
            Enforce.NotNullOrEmpty(screenName);

            var client = CreateClient();
            client.Url = ApiAuthority + "/1.1/friendships/create.json";
            client.Parameters.Add("screen_name", screenName);
            client.Parameters.Add("follow", "true");
            return client.Post();
        }

        public IObservable<WebResponse> Unfollow(string screenName)
        {
            Enforce.NotNullOrEmpty(screenName);

            var client = CreateClient();
            client.Url = ApiAuthority + "/1.1/friendships/destroy.json";
            client.Parameters.Add("screen_name", screenName);
            return client.Post();
        }

        public IObservable<List> GetLists(string screenName)
        {
            return GetContents(ApiAuthority, "/1.1/lists/list.json", new { screen_name = screenName })
                .SelectJsonArrayToManyJsonValue()
                .Select(JsonHelper.ToList)
                .WhereNotNull();
        }

        public IObservable<Tweet> GetListStatuses(string id)
        {
            return GetContents(ApiAuthority, "/1.1/lists/statuses.json", new { list_id = id }, new { include_entities = "1" }, new { include_rts = "1" })
                .SelectJsonArrayToManyJsonValue()
                .SelectTweets(_subject);
        }

        public IObservable<User> GetAccountVerification()
        {
            return GetContents(ApiAuthority, "/1.1/account/verify_credentials.json")
                .SelectValidJsonValue()
                .Select(x => new User(x));
        }

        public IObservable<RateLimitStatus> GetRateLimitStatus()
        {
            return GetContents(ApiAuthority, "/1.1/application/rate_limit_status.json")
                .SelectValidJsonValue()
                .Select(x => new RateLimitStatus(x));
        }

        public IObservable<Relationship> GetRelationship(string sourceScreenName, string targetScreenName)
        {
            return GetContents(ApiAuthority, "/1.1/friendships/show.json", new { source_screen_name = sourceScreenName }, new { target_screen_name = targetScreenName })
                .SelectValidJsonValue()
                .Select(JsonHelper.ToRelationship)
                .WhereNotNull();
        }

        public IObservable<User> GetUserInfo(string screenName)
        {
            return GetContents(ApiAuthority, "/1.1/users/show.json", new { include_entities = "1" }, new { screen_name = screenName })
                .SelectValidJsonValue()
                .Select(JsonHelper.ToUser)
                .WhereNotNull();
        }

        public IObservable<Tweet> GetHomeTimeline(int count = RequestCount)
        {
            var options = new object[] { new { include_entities = "1" }, new { include_rts = "1" }, new { count } };
            return GetSnapshot(ApiAuthority, "/1.1/statuses/home_timeline.json", options).SelectTweets(_subject);
        }

        public IObservable<Tweet> GetCurrentUserTimeline(int count = RequestCount)
        {
            var options = new object[] { new { include_entities = "1" }, new { include_rts = "1" }, new { count } };
            return GetSnapshot(ApiAuthority, "/1.1/statuses/user_timeline.json", options).SelectTweets(_subject);
        }

        public IObservable<Tweet> GetUserTimeline(string screenName, string sinceId = null)
        {
            var options = new object[] { new { screen_name = screenName }, new { include_entities = "1" }, new { include_rts = "1" }, new { since_id = sinceId } };
            return GetSnapshot(ApiAuthority, "/1.1/statuses/user_timeline.json", options).SelectTweets(_subject);
        }

        public IObservable<Tweet> GetTweet(string id)
        {
            return GetContents(ApiAuthority, string.Format("/1.1/statuses/show/{0}.json", id), new { include_entities = "1" })
                .SelectValidJsonValue()
                .SelectTweets(_subject);
        }

        public IObservable<SearchResult> GetSearch(string query, string sinceId = null, int count = RequestCount)
        {
            var options = new object[] { new { include_entities = "1" }, new { q = query }, new { count }, new { since_id = sinceId } };
            return GetContents(SearchAuthority, "/search.json", options)
                .SelectValidJsonValue()
                .SelectMany(x => (JsonArray)x["results"])
                .SelectSearchResults(_subject);
        }

        public IObservable<Tweet> GetStreamingHomeline()
        {
            return GetStreaming(UserStreamingAuthority, "/2/user.json");
        }

        public IObservable<Tweet> GetMentions(int count = RequestCount)
        {
            var options = new object[] { new { count }, new { include_entities = "1" } };
            return GetSnapshot(ApiAuthority, "/1.1/statuses/mentions_timeline.json", options).SelectTweets(_subject);
        }

        public IObservable<DirectMessage> GetDirectMessages(string sinceId = null)
        {
            var options = new object[] { new { since_id = sinceId }, new { include_entities = "1" } };
            return GetSnapshot(ApiAuthority, "/1.1/direct_messages.json", options).SelectDirectMessages(_subject);
        }

        public IObservable<Tweet> GetFavorites(string sinceId = null)
        {
            var options = new object[] { new { since_id = sinceId }, new { include_entities = "1" } };
            return GetSnapshot(ApiAuthority, "/1.1/favorites/list.json", options).SelectTweets(_subject);
        }

        public IObservable<Tweet> GetStreamingSampling()
        {
            return GetStreaming(StreamingAuthority, "/1.1/statuses/sample.json", new { delimited = "length" });
        }

        public IObservable<Tweet> GetStreamingFilter(params string[] terms)
        {
            return GetStreaming(StreamingAuthority, "/1.1/statuses/filter.json", new { track = string.Join(",", terms) });
        }

        private IObservable<JsonValue> GetSnapshot(string authority, string path, params object[] parameters)
        {
            return GetContents(authority, path, parameters).SelectJsonArrayToManyJsonValue();
        }

        private IObservable<Tweet> GetStreaming(string authority, string path, params object[] parameters)
        {
            return GetContents(authority, path, parameters)
                .SelectValidJsonValue()
                .SelectTweets(_subject);
        }

        private IObservable<string> GetContents(string authority, string path, params object[] parameters)
        {
            var client = CreateClient();
            client.Url = authority + path;
            ParseParameters(client, parameters);
            return client.Get().GetResponseLines();
        }

        private static void ParseParameters(OAuthClient request, IEnumerable<object> parameters)
        {
            foreach (var param in parameters)
            {
                var prop = param.GetType().GetProperties().Single();
                var value = prop.GetValue(param, null);
                if (value != null)
                    request.Parameters.Add(prop.Name, value.ToString());
            }
        }

        IDisposable IObservable<ITweetItem>.Subscribe(IObserver<ITweetItem> observer)
        {
            return _subject.Subscribe(observer);
        }
    }
}