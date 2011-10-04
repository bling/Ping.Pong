using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using PingPong.Core;

namespace PingPong.OAuth
{
    /// <summary>OAuth authenticated client.</summary>
    public class OAuthClient : OAuthBase
    {
        public AccessToken AccessToken { get; private set; }
        public IDictionary<string, object> Parameters { get; private set; }
        public string Url { get; set; }
        public string Realm { get; set; }

        public OAuthClient(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret)
            : this(consumerKey, consumerSecret, new AccessToken(accessToken, accessTokenSecret))
        {
        }

        public OAuthClient(string consumerKey, string consumerSecret, AccessToken accessToken)
            : base(consumerKey, consumerSecret)
        {
            Enforce.NotNull(accessToken, "accessToken");

            AccessToken = accessToken;
            Parameters = new Dictionary<string, object>();
        }

        private string GetAuthorizationHeader(MethodType methodType)
        {
            var realm = (Realm != null) ? new[] { new KeyValuePair<string, object>("realm", Realm) } : Enumerable.Empty<KeyValuePair<string, object>>();
            var parameters = ConstructBasicParameters(Url, methodType, AccessToken, Parameters.ToArray());
            return BuildAuthorizationHeader(realm.Concat(parameters));
        }

        private WebRequest CreateWebRequest(MethodType methodType)
        {
            Enforce.NotNullOrEmpty(Url);
            string requestUrl = (methodType == MethodType.Get) ? Url + "?" + Parameters.ToQueryParameter() : Url;

            var req = WebRequest.CreateHttp(requestUrl);
            req.Headers[HttpRequestHeader.Authorization] = GetAuthorizationHeader(methodType);
            req.Headers["X-User-Agent"] = AppBootstrapper.UserAgentVersion;
            req.Method = methodType.ToString().ToUpper();
            if (methodType == MethodType.Post)
                req.ContentType = "application/x-www-form-urlencoded";

            return req;
        }

        /// <summary>Asynchronously get the web response.</summary>
        public IObservable<WebResponse> Get()
        {
            return CreateWebRequest(MethodType.Get).GetResponseAsObservable();
        }

        public IObservable<WebResponse> Post()
        {
            return Observable.Start(() =>
            {
                var postData = Encoding.UTF8.GetBytes(Parameters.ToQueryParameter());
                var req = CreateWebRequest(MethodType.Post);
                return req.GetRequestStreamAsObservable()
                    .Do(stream => { using (stream) stream.Write(postData, 0, postData.Length); })
                    .SelectMany(_ => req.GetResponseAsObservable())
                    .First();
            });
        }
    }
}