using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Controls;
using Caliburn.Micro;
using Hammock;
using Hammock.Authentication.OAuth;
using PingPong.Messages;

namespace PingPong
{
    public class AuthorizationViewModel : Screen
    {
        private readonly IEventAggregator _eventAggregator;

        private AuthInfo _auth;
        private string _pin;

        public string Pin
        {
            get { return _pin; }
            set { this.SetValue("Pin", value, ref _pin); }
        }

        public AuthorizationViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        public void LoadWebBrowser(WebBrowser browser)
        {
            Observable.Create<RestResponse>(
                obs =>
                {
                    var client = new RestClient
                    {
                        Authority = "https://api.twitter.com/oauth",
                        Credentials = new OAuthCredentials
                        {
                            Type = OAuthType.RequestToken,
                            SignatureMethod = OAuthSignatureMethod.HmacSha1,
                            ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader,
                            ConsumerKey = AppBootstrapper.ConsumerKey,
                            ConsumerSecret = AppBootstrapper.ConsumerSecret,
                            Version = "1.0",
                            CallbackUrl = "oob",
                        },
                        HasElevatedPermissions = true
                    };

                    client.BeginRequest(new RestRequest { Path = "/request_token" }, (request, response, state) => obs.OnNext(response));
                    return Disposable.Empty;
                })
                .Select(x =>
                {
                    var query = x.Content.ToQueryParameters();
                    return new AuthInfo { AuthToken = query["oauth_token"], AuthTokenSecret = query["oauth_token_secret"] };
                })
                .Do(auth => _auth = auth)
                .DispatcherSubscribe(auth => browser.Navigate(new Uri("https://api.twitter.com/oauth/authorize?oauth_token=" + auth.AuthToken)));
        }

        public void AuthenticatePin()
        {
            int pin;
            if (!int.TryParse(Pin, out pin))
                throw new InvalidOperationException("The PIN must be a number.");

            Observable.Create<RestResponse>(
                obs =>
                {
                    var client = new RestClient
                    {
                        Authority = "http://twitter.com/oauth",
                        Credentials = new OAuthCredentials
                        {
                            Type = OAuthType.AccessToken,
                            SignatureMethod = OAuthSignatureMethod.HmacSha1,
                            ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader,
                            ConsumerKey = AppBootstrapper.ConsumerKey,
                            ConsumerSecret = AppBootstrapper.ConsumerSecret,
                            Token = _auth.AuthToken,
                            TokenSecret = _auth.AuthTokenSecret,
                            Verifier = Pin,
                        }
                    };
                    client.BeginRequest(new RestRequest { Path = "/access_token" }, (request, response, state) => obs.OnNext(response));
                    return Disposable.Empty;
                })
                .Select(x =>
                {
                    var query = x.Content.ToQueryParameters();
                    return new AuthInfo { AuthToken = query["oauth_token"], AuthTokenSecret = query["oauth_token_secret"] };
                })
                .Subscribe(auth =>
                {
                    AppSettings.UserOAuthToken = auth.AuthToken;
                    AppSettings.UserOAuthTokenSecret = auth.AuthTokenSecret;
                    _eventAggregator.Publish(new ShowTimelinesMessage());
                });
        }
    }

    public class AuthInfo
    {
        public string AuthToken { get; set; }
        public string AuthTokenSecret { get; set; }
    }
}