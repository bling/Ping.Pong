using System;
using System.Collections.Generic;
using System.Json;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Caliburn.Micro;
using PingPong.Models;
using PingPong.OAuth;

namespace PingPong.Core
{
    public class TwitterClient : IObservable<Tweet>, IDisposable
    {
        private const int RequestCount = 200;
        private const string ApiAuthority = "https://api.twitter.com";
        private const string SearchAuthority = "https://search.twitter.com";
        private const string StreamingAuthority = "https://stream.twitter.com";
        private const string UserStreamingAuthority = "https://userstream.twitter.com";

        private readonly Subject<Tweet> _tweets = new Subject<Tweet>();
        private static readonly ILog _log = LogManager.GetLog(typeof(TwitterClient));

        public TwitterClient()
        {
            if (!AppSettings.HasAuthToken)
                throw new InvalidOperationException("Not authorized yet.");
        }

        public void Dispose()
        {
            _tweets.Dispose();
        }

        private OAuthClient CreateClient()
        {
            return new OAuthClient(AppBootstrapper.ConsumerKey, AppBootstrapper.ConsumerSecret, AppSettings.UserOAuthToken, AppSettings.UserOAuthTokenSecret);
        }

        public void UpdateStatus(string text, ulong? inReplyToStatusId = null)
        {
            Enforce.NotNullOrEmpty(text);
            var client = CreateClient();
            client.Parameters["status"] = text;
            client.Parameters["wrap_links"] = "1";
            if (inReplyToStatusId != null)
                client.Parameters["in_reply_to_status_id"] = inReplyToStatusId.Value;
            
            client.Url = ApiAuthority + "/1/statuses/update.json";
            client.Post();
        }

        public void Retweet(ulong statusId)
        {
            var client = CreateClient();
            client.Url = ApiAuthority + string.Format("/1/statuses/retweet/{0}.json", statusId);
            client.Post();
        }

        public void DirectMessage(string username, string text)
        {
            Enforce.NotNullOrEmpty(username);
            Enforce.NotNullOrEmpty(text);

            var client = CreateClient();
            client.Url = ApiAuthority + "/1/direct_messages/new.json";
            client.Parameters.Add("screen_name", username);
            client.Parameters.Add("text", text);
            client.Post();
        }

        public void Follow(string screenName)
        {
            Enforce.NotNullOrEmpty(screenName);

            var client = CreateClient();
            client.Url = ApiAuthority + "/1/friendships/create.json";
            client.Parameters.Add("screen_name", screenName);
            client.Parameters.Add("follow", "true");
            client.Post();
        }

        public void Unfollow(string screenName)
        {
            Enforce.NotNullOrEmpty(screenName);

            var client = CreateClient();
            client.Url = ApiAuthority + "/1/friendships/destroy.json";
            client.Parameters.Add("screen_name", screenName);
            client.Post();
        }

        public IObservable<User> GetAccountVerification()
        {
            return GetContents(ApiAuthority, "/1/account/verify_credentials.json")
                .Select(ToJson)
                .Where(x => x != null)
                .Select(x => new User(x));
        }

        public IObservable<User> GetAccountInfo(string screenName)
        {
            return GetContents(ApiAuthority, "/1/users/lookup.json", new { screen_name = screenName })
                .Select(ToJson)
                .Where(x => x != null)
                .Cast<JsonArray>()
                .SelectMany(x => x)
                .Select(x => new User(x));
        }

        public IObservable<Relationship> GetRelationship(string sourceScreenName, string targetScreenName)
        {
            return GetContents(ApiAuthority, "/1/friendships/show.json", new { source_screen_name = sourceScreenName }, new { target_screen_name = targetScreenName })
                .Select(ToJson)
                .Where(x => x != null)
                .Select(JsonHelper.ToRelationship)
                .Where(x => x != null);
        }

        public IObservable<User> GetUserInfo(string screenName)
        {
            return GetContents(ApiAuthority, "/1/users/show.json", new { include_entities = "1" }, new { screen_name = screenName })
                .Select(ToJson)
                .Where(x => x != null)
                .Select(JsonHelper.ToUser)
                .Where(x => x != null);
        }

        public IObservable<Tweet> GetHomeTimeline(int count = RequestCount)
        {
            var options = new object[] { new { include_entities = "1" }, new { include_rts = "1" }, new { count } };
            return GetSnapshot(ApiAuthority, "/statuses/home_timeline.json", options).SelectTweets(_tweets);
        }

        public IObservable<Tweet> GetCurrentUserTimeline(int count = RequestCount)
        {
            var options = new object[] { new { include_entities = "1" }, new { include_rts = "1" }, new { count } };
            return GetSnapshot(ApiAuthority, "/1/statuses/user_timeline.json", options).SelectTweets(_tweets);
        }

        public IObservable<Tweet> GetUserTimeline(string screenName, ulong? sinceId = null)
        {
            var options = new object[] { new { screen_name = screenName }, new { include_entities = "1" }, new { include_rts = "1" }, new { since_id = sinceId } };
            return GetSnapshot(ApiAuthority, "/1/statuses/user_timeline.json", options).SelectTweets(_tweets);
        }

        public IObservable<SearchResult> GetSearch(string query, ulong? sinceId = null, int count = RequestCount)
        {
            var options = new object[] { new { include_entities = "1" }, new { q = query.UrlEncode() }, new { count }, new { since_id = sinceId } };
            return GetContents(SearchAuthority, "/search.json", options)
                .Select(ToJson)
                .SelectMany(x => (JsonArray)x["results"])
                .SelectSearchResults();
        }

        public IObservable<Tweet> GetStreamingHomeline()
        {
            return GetStreaming(UserStreamingAuthority, "/2/user.json");
        }

        public IObservable<Tweet> GetMentions(int count = RequestCount)
        {
            var options = new object[] { new { include_rts = "1" }, new { count }, new { include_entities = "1" } };
            return GetSnapshot(ApiAuthority, "/statuses/mentions.json", options).SelectTweets(_tweets);
        }

        public IObservable<DirectMessage> GetDirectMessages(ulong? sinceId = null)
        {
            var options = new object[] { new { since_id = sinceId }, new { include_entities = "1" } };
            return GetSnapshot(ApiAuthority, "/direct_messages.json", options).SelectDirectMessages();
        }

        public IObservable<Tweet> GetFavorites(ulong? sinceId = null)
        {
            var options = new object[] { new { since_id = sinceId }, new { include_entities = "1" } };
            return GetSnapshot(ApiAuthority, "/favorites.json", options).SelectTweets(_tweets);
        }

        public IObservable<Tweet> GetStreamingSampling()
        {
            return GetStreaming(StreamingAuthority, "/1/statuses/sample.json", new { delimited = "length" });
        }

        public IObservable<Tweet> GetStreamingFilter(params string[] terms)
        {
            return GetStreaming(StreamingAuthority, "/1/statuses/filter.json", new { track = string.Join(",", terms) });
        }

        private IObservable<JsonValue> GetSnapshot(string authority, string path, params object[] parameters)
        {
            return GetContents(authority, path, parameters)
                .Select(x => (JsonArray)ToJson(x))
                .SelectMany(x => x);
        }

        private IObservable<Tweet> GetStreaming(string authority, string path, params object[] parameters)
        {
            return GetContents(authority, path, parameters)
                .Select(ToJson)
                .SelectTweets(_tweets)
                .Buffer(TimeSpan.FromMilliseconds(100))
                .SelectMany(x => x);
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

        private static JsonValue ToJson(string content)
        {
            try
            {
                return JsonValue.Parse(content);
            }
            catch (Exception e)
            {
                _log.Error(e);
            }
            return null;
        }

        IDisposable IObservable<Tweet>.Subscribe(IObserver<Tweet> observer)
        {
            return _tweets.Subscribe(observer);
        }
    }
}