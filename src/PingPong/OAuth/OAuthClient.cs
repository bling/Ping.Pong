using System;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using PingPong.Core;

namespace PingPong.OAuth
{
    /// <summary>OAuth authenticated client.</summary>
    public class OAuthClient : OAuthBase
    {
        public AccessToken AccessToken { get; private set; }
        public ParameterCollection Parameters { get; private set; }
        public string Url { get; set; }
        public string Realm { get; set; }
        public MethodType MethodType { get; set; }

        public OAuthClient(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret)
            : this(consumerKey, consumerSecret, new AccessToken(accessToken, accessTokenSecret))
        {
        }

        public OAuthClient(string consumerKey, string consumerSecret, AccessToken accessToken)
            : base(consumerKey, consumerSecret)
        {
            Enforce.NotNull(accessToken, "accessToken");

            AccessToken = accessToken;
            Parameters = new ParameterCollection();
            MethodType = MethodType.Get;
        }

        private string AuthorizationHeader
        {
            get
            {
                var realm = (Realm != null) ? new[] { new Parameter("realm", Realm) } : Enumerable.Empty<Parameter>();
                var parameters = ConstructBasicParameters(Url, MethodType, AccessToken, Parameters);
                return BuildAuthorizationHeader(realm.Concat(parameters));
            }
        }

        private WebRequest CreateWebRequest()
        {
            string requestUrl = (MethodType == MethodType.Get) ? Url + "?" + Parameters.ToQueryParameter() : Url;

            var req = WebRequest.CreateHttp(requestUrl);
            req.Headers[HttpRequestHeader.Authorization] = AuthorizationHeader;
            req.Headers["X-User-Agent"] = AppBootstrapper.UserAgentVersion;
            req.Method = MethodType.ToString().ToUpper();
            if (MethodType == MethodType.Post)
                req.ContentType = "application/x-www-form-urlencoded";

            return req;
        }

        /// <summary>Asynchronously get the web response.</summary>
        public IObservable<WebResponse> GetResponse()
        {
            if (Url == null) throw new InvalidOperationException("The Url is not set.");

            var req = CreateWebRequest();
            switch (MethodType)
            {
                case MethodType.Get:
                    return req.GetResponseAsObservable();
                case MethodType.Post:
                    var postData = Encoding.UTF8.GetBytes(Parameters.ToQueryParameter());
                    return req.GetRequestStreamAsObservable()
                        .Do(stream =>
                        {
                            stream.Write(postData, 0, postData.Length);
                            stream.Close();
                        })
                        .SelectMany(_ => req.GetResponseAsObservable());
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}