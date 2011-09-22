using System;
using System.Collections.Generic;
using System.Json;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using Caliburn.Micro;
using Hammock;
using Hammock.Authentication.OAuth;
using Hammock.Streaming;
using Hammock.Web;
using PingPong.Models;

namespace PingPong.Core
{
    public class TwitterClient
    {
        private const string RequestCount = "20";
        private const string ApiAuthority = "https://api.twitter.com";
        private const string SearchAuthority = "https://search.twitter.com";
        private const string StreamingAuthority = "https://stream.twitter.com";
        private const string UserStreamingAuthority = "https://userstream.twitter.com";

        private readonly OAuthCredentials _credentials;
        private static readonly ILog _log = LogManager.GetLog(typeof(TwitterClient));

        public TwitterClient()
        {
            if (!AppSettings.HasAuthToken)
                throw new InvalidOperationException("Not authorized yet.");

            _credentials = new OAuthCredentials
            {
                Type = OAuthType.ProtectedResource,
                SignatureMethod = OAuthSignatureMethod.HmacSha1,
                ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader,
                ConsumerKey = AppBootstrapper.ConsumerKey,
                ConsumerSecret = AppBootstrapper.ConsumerSecret,
                Token = AppSettings.UserOAuthToken,
                TokenSecret = AppSettings.UserOAuthTokenSecret,
                Version = "1.0",
            };
        }

        private RestClient CreateClient(string authority)
        {
            return new RestClient
            {
                Credentials = _credentials,
                Authority = authority,
                HasElevatedPermissions = Application.Current.HasElevatedPermissions,
                SilverlightUserAgentHeader = AppBootstrapper.UserAgentVersion
            };
        }

        public void UpdateStatus(string text, ulong? inReplyToStatusId = null)
        {
            Enforce.NotNullOrEmpty(text);
            var request = new RestRequest { Credentials = _credentials, Method = WebMethod.Post, Path = "/1/statuses/update.json" };
            request.AddParameter("status", text);
            request.AddParameter("wrap_links", "true");
            if (inReplyToStatusId != null)
                request.AddParameter("in_reply_to_status_id", inReplyToStatusId.ToString());

            CreateClient(ApiAuthority).BeginRequest(request);
        }

        public void Retweet(ulong statusId)
        {
            var request = new RestRequest
            {
                Credentials = _credentials,
                Method = WebMethod.Post,
                Path = string.Format("/1/statuses/retweet/{0}.json", statusId),
            };
            CreateClient(ApiAuthority).BeginRequest(request);
        }

        public void DirectMessage(string username, string text)
        {
            Enforce.NotNullOrEmpty(username);
            Enforce.NotNullOrEmpty(text);

            var request = new RestRequest { Credentials = _credentials, Method = WebMethod.Post, Path = "/1/direct_messages/new.json" };
            request.AddParameter("screen_name", username);
            request.AddParameter("text", text);
            CreateClient(ApiAuthority).BeginRequest(request);
        }

        public IObservable<Tweet> GetHomeTimeline()
        {
            return GetSnapshot(ApiAuthority, "/statuses/home_timeline.json", null, Tuple.Create("include_rts", "1"));
        }

        public IObservable<Tweet> GetUserTimeline(string screenName, ulong? sinceId = null)
        {
            return GetSnapshot(ApiAuthority, "/1/statuses/user_timeline.json", sinceId, Tuple.Create("screen_name", screenName), Tuple.Create("include_rts", "1"));
        }

        public IObservable<Tweet> GetSearch(string query, ulong? sinceId = null)
        {
            return GetSnapshot(SearchAuthority, "/search.json", sinceId, Tuple.Create("q", query));
        }

        public IObservable<Tweet> GetStreamingHomeline()
        {
            return GetStreaming(UserStreamingAuthority, "/2/user.json");
        }

        public IObservable<Tweet> GetMentions(ulong? sinceId)
        {
            return GetSnapshot(ApiAuthority, "/statuses/mentions.json", sinceId, Tuple.Create("include_rts", "1"));
        }

        public IObservable<Tweet> GetDirectMessages(ulong? sinceId)
        {
            return GetSnapshot(ApiAuthority, "/direct_messages.json", sinceId);
        }

        public IObservable<Tweet> GetFavorites(ulong? sinceId)
        {
            return GetSnapshot(ApiAuthority, "/favorites.json", sinceId);
        }

        public IObservable<Tweet> GetStreamingSampling()
        {
            return GetStreaming(StreamingAuthority, "/1/statuses/sample.json", Tuple.Create("delimited", "length"));
        }

        public IObservable<Tweet> GetStreamingFilter(params string[] terms)
        {
            return GetStreaming(StreamingAuthority, "/1/statuses/filter.json", Tuple.Create("track", string.Join(",", terms)));
        }

        private IObservable<Tweet> GetSnapshot(string authority, string path, ulong? sinceId, params Tuple<string, string>[] queryParameters)
        {
            return Observable.Create<Tweet>(
                ob =>
                {
                    var request = new RestRequest { Path = path };
                    queryParameters.Where(qp => qp != null).ForEach(qp => request.AddParameter(qp.Item1, qp.Item2));
                    request.AddParameter("count", RequestCount);
                    if (sinceId != null)
                        request.AddParameter("since_id", sinceId.ToString());

                    CreateClient(authority).BeginRequest(request, (_, r, __) =>
                    {
                        ToTweets(r).ForEach(ob.OnNext);
                        ob.OnCompleted();
                    });
                    return Disposable.Empty;
                });
        }

        private IObservable<Tweet> GetStreaming(string authority, string path, params Tuple<string, string>[] queryParameters)
        {
            return Observable.Create<RestResponse>(
                ob =>
                {
                    var request = new RestRequest { Path = path, Credentials = _credentials, Method = WebMethod.Get, StreamOptions = new StreamOptions { ResultsPerCallback = 1 } };
                    queryParameters.Where(qp => qp != null).ForEach(qp => request.AddParameter(qp.Item1, qp.Item2));
                    var client = CreateClient(authority);
                    client.BeginRequest(request, (_, r, __) => ob.OnNext(r));
                    return Disposable.Create(client.CancelStreaming);
                })
                .SelectMany(ToTweets)
                .Buffer(TimeSpan.FromMilliseconds(100))
                .SelectMany(x => x);
        }

        private static IEnumerable<Tweet> ToTweets(RestResponse response)
        {
            JsonValue json = null;
            try
            {
                json = JsonValue.Parse(response.Content);
            }
            catch (Exception e)
            {
                _log.Error(e);
            }

            return ToTweets(json).Where(x => x != null);
        }

        private static IEnumerable<Tweet> ToTweets(JsonValue json)
        {
            var array = json as JsonArray;
            if (array != null)
            {
                foreach (var value in array)
                    yield return Tweet.TryParse(value);
            }
            else
            {
                yield return Tweet.TryParse(json);
            }
        }
    }
}