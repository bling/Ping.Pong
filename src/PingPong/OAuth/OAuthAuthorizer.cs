using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using PingPong.Core;

namespace PingPong.OAuth
{
    /// <summary>OAuth authorization client.</summary>
    public class OAuthAuthorizer : OAuthBase
    {
        public OAuthAuthorizer(string consumerKey, string consumerSecret)
            : base(consumerKey, consumerSecret)
        {
        }

        private IObservable<TokenResponse<T>> GetTokenResponse<T>(string url, IEnumerable<Parameter> parameters, Func<string, string, T> tokenFactory) where T : Token
        {
            var req = WebRequest.CreateHttp(url);
            req.Headers[HttpRequestHeader.Authorization] = BuildAuthorizationHeader(parameters);
            req.Method = MethodType.Post.ToString().ToUpper();
            req.ContentType = "application/x-www-form-urlencoded";

            return req.GetResponseAsObservable()
                .SelectMany(x => x.GetLines())
                .Select(tokenBase =>
                {
                    var splitted = tokenBase.Split('&').Select(s => s.Split('=')).ToDictionary(s => s.First(), s => s.Last());
                    var token = tokenFactory(splitted["oauth_token"], splitted["oauth_token_secret"]);
                    var extraData = splitted.Where(kvp => kvp.Key != "oauth_token" && kvp.Key != "oauth_token_secret")
                        .ToLookup(kvp => kvp.Key, kvp => kvp.Value);
                    return new TokenResponse<T>(token, extraData);
                });
        }

        public string BuildAuthorizeUrl(string authUrl, RequestToken requestToken)
        {
            Enforce.NotNull(authUrl, "authUrl");
            Enforce.NotNull(requestToken, "accessToken");

            return authUrl + "?oauth_token=" + requestToken.Key;
        }

        /// <summary>Asynchronously gets request tokens.</summary>
        /// <param name="otherParameters">need parameters except consumer_key,timestamp,nonce,signature,signature_method,version</param>
        public IObservable<TokenResponse<RequestToken>> GetRequestToken(string requestTokenUrl, params Parameter[] otherParameters)
        {
            Enforce.NotNull(requestTokenUrl, "requestTokenUrl");
            Enforce.NotNull(otherParameters, "otherParameters");

            var parameters = ConstructBasicParameters(requestTokenUrl, MethodType.Post, null, otherParameters);
            parameters.Add(otherParameters);
            return GetTokenResponse(requestTokenUrl, parameters, (key, secret) => new RequestToken(key, secret));
        }
    }
}