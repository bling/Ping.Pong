using System;
using System.Windows;
using Caliburn.Micro;
using PingPong.Messages;

namespace PingPong
{
    public class ShellViewModel : Conductor<object>,
                                  IShell,
                                  IHandle<ShowTimelinesMessage>
    {
        private readonly Func<TimelinesViewModel> _timelinesFactory;
        private readonly Func<AuthorizationViewModel> _authorizationFactory;
        private readonly Func<InstallViewModel> _installerFactory;

        public ShellViewModel(Func<TimelinesViewModel> timelinesFactory,
                              Func<AuthorizationViewModel> authorizationFactory,
                              Func<InstallViewModel> installerFactory)
        {
            _timelinesFactory = timelinesFactory;
            _authorizationFactory = authorizationFactory;
            _installerFactory = installerFactory;
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            if (string.IsNullOrEmpty(AppBootstrapper.ConsumerKey) || string.IsNullOrEmpty(AppBootstrapper.ConsumerSecret))
            {
                ActivateItem(new ErrorViewModel("Please create your own consumer key/secret from Twitter."));
            }
            else if (Application.Current.IsRunningOutOfBrowser)
            {
                ActivateItem(AppSettings.HasAuthToken
                                 ? (object)_timelinesFactory()
                                 : _authorizationFactory());
            }
            else
            {
                ActivateItem(_installerFactory());
            }
        }

        void IHandle<ShowTimelinesMessage>.Handle(ShowTimelinesMessage message)
        {
            ActivateItem(_timelinesFactory());
        }
    }
}