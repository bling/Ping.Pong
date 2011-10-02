using System;
using System.Reactive.Linq;
using System.Windows.Controls;
using Caliburn.Micro;
using PingPong.Core;
using PingPong.Messages;
using PingPong.OAuth;

namespace PingPong.ViewModels
{
    public class AuthorizationViewModel : Screen
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IWindowManager _windowManager;

        private string _pin;
        private RequestToken _token;

        public string Pin
        {
            get { return _pin; }
            set { this.SetValue("Pin", value, ref this._pin); }
        }

        public AuthorizationViewModel(IEventAggregator eventAggregator, IWindowManager windowManager)
        {
            _eventAggregator = eventAggregator;
            _windowManager = windowManager;
        }

        public void LoadWebBrowser(WebBrowser browser)
        {
            var authorizer = new OAuthAuthorizer(AppBootstrapper.ConsumerKey, AppBootstrapper.ConsumerSecret);
            authorizer.GetRequestToken("https://api.twitter.com/oauth/request_token")
                .Select(x => x.Token)
                .DispatcherSubscribe(
                    token =>
                    {
                        _token = token;
                        string url = authorizer.BuildAuthorizeUrl("https://api.twitter.com/oauth/authorize", token);
                        browser.Navigate(new Uri(url));
                    },
                    OnError);
        }

        public void AuthenticatePin()
        {
            Enforce.NotNull(_token);

            int pin;
            if (!int.TryParse(Pin, out pin))
                throw new InvalidOperationException("The PIN must be a number.");

            var authorizer = new OAuthAuthorizer(AppBootstrapper.ConsumerKey, AppBootstrapper.ConsumerSecret);
            authorizer.GetAccessToken("https://twitter.com/oauth/access_token", _token, Pin)
                .DispatcherSubscribe(
                    response =>
                    {
                        AppSettings.UserOAuthToken = response.Token.Key;
                        AppSettings.UserOAuthTokenSecret = response.Token.Secret;
                        _eventAggregator.Publish(new ShowTimelinesMessage());
                    },
                    OnError);
        }

        private void OnError(Exception ex)
        {
            _windowManager.ShowDialog(new ErrorViewModel(ex.ToString()));
        }
    }
}