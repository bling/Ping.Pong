﻿using System;
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
        private const int RequestCount = 20;
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

        public IObservable<JsonValue> GetCredentialVerification()
        {
            return GetContents(false, ApiAuthority, "/1/account/verify_credentials.json")
                .Select(ToJson)
                .Where(x => x != null);
        }

        public IObservable<Tweet> GetHomeTimeline(int count = RequestCount)
        {
            return GetSnapshot(ApiAuthority, "/statuses/home_timeline.json", new { include_rts = "1" }, new { count });
        }

        public IObservable<Tweet> GetCurrentUserTimeline(int count = RequestCount)
        {
            return GetSnapshot(ApiAuthority, "/1/statuses/user_timeline.json", new { include_rts = "1" }, new { count });
        }

        public IObservable<Tweet> GetUserTimeline(string screenName, ulong? sinceId = null)
        {
            return GetSnapshot(ApiAuthority, "/1/statuses/user_timeline.json", new { screen_name = screenName }, new { include_rts = "1" }, new { since_id = sinceId });
        }

        public IObservable<Tweet> GetSearch(string query, ulong? sinceId = null, int count = RequestCount)
        {
            return GetSnapshot(SearchAuthority, "/search.json", new { q = query }, new { count }, new { since_id = sinceId });
        }

        public IObservable<Tweet> GetStreamingHomeline()
        {
            return GetStreaming(UserStreamingAuthority, "/2/user.json");
        }

        public IObservable<Tweet> GetMentions(int count = RequestCount)
        {
            return GetSnapshot(ApiAuthority, "/statuses/mentions.json", new { include_rts = "1" }, new { count });
        }

        public IObservable<Tweet> GetDirectMessages(ulong? sinceId = null)
        {
            return GetSnapshot(ApiAuthority, "/direct_messages.json", new { since_id = sinceId });
        }

        public IObservable<Tweet> GetFavorites(ulong? sinceId = null)
        {
            return GetSnapshot(ApiAuthority, "/favorites.json", new { since_id = sinceId });
        }

        public IObservable<Tweet> GetStreamingSampling()
        {
            return GetStreaming(StreamingAuthority, "/1/statuses/sample.json", new { delimited = "length" });
        }

        public IObservable<Tweet> GetStreamingFilter(params string[] terms)
        {
            return GetStreaming(StreamingAuthority, "/1/statuses/filter.json", new { track = string.Join(",", terms) });
        }

        private IObservable<Tweet> GetSnapshot(string authority, string path, params object[] parameters)
        {
            return GetContents(false, authority, path, parameters).SelectMany(ToTweets);
        }

        private IObservable<Tweet> GetStreaming(string authority, string path, params object[] parameters)
        {
            return GetContents(true, authority, path, parameters)
                .SelectMany(ToTweets)
                .Buffer(TimeSpan.FromMilliseconds(100))
                .SelectMany(x => x);
        }

        private IObservable<string> GetContents(bool streaming, string authority, string path, params object[] parameters)
        {
            return Observable.Create<string>(
                ob =>
                {
                    var request = new RestRequest { Path = path };
                    ParseParameters(request, parameters);
                    if (streaming)
                        request.StreamOptions = new StreamOptions { ResultsPerCallback = 1 };

                    var client = CreateClient(authority);
                    client.BeginRequest(request, (_, r, __) => ob.OnNext(r.Content));

                    return streaming ? Disposable.Create(client.CancelStreaming) : Disposable.Empty;
                });
        }

        private static void ParseParameters(RestRequest request, IEnumerable<object> parameters)
        {
            foreach (var param in parameters)
            {
                var prop = param.GetType().GetProperties().Single();
                var value = prop.GetValue(param, null);
                if (value != null)
                    request.AddParameter(prop.Name, value.ToString());
            }
        }

        private static IEnumerable<Tweet> ToTweets(string content)
        {
            return ToTweets(ToJson(content)).Where(x => x != null);
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
    }
}