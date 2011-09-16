using System.Windows;
using Caliburn.Micro;
using PingPong.Messages;

namespace PingPong
{
    public class ShellViewModel : Conductor<object>,
                                  IShell,
                                  IHandle<NavigateToUserMessage>,
                                  IHandle<NavigateToTopicMessage>,
                                  IHandle<ShowAuthorizationMessage>,
                                  IHandle<ShowTimelinesMessage>
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IWindowManager _windowManager;

        public ShellViewModel(IEventAggregator eventAggregator, IWindowManager windowManager)
        {
            _eventAggregator = eventAggregator;
            _windowManager = windowManager;
            _eventAggregator.Subscribe(this);
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
                _eventAggregator.Publish(AppSettings.HasAuthToken
                                             ? (object)new ShowTimelinesMessage()
                                             : new ShowAuthorizationMessage());
            }
            else
            {
                ActivateItem(IoC.Get<InstallViewModel>());
            }
        }

        void IHandle<ShowAuthorizationMessage>.Handle(ShowAuthorizationMessage message)
        {
            ActivateItem(IoC.Get<AuthorizationViewModel>());
        }

        void IHandle<ShowTimelinesMessage>.Handle(ShowTimelinesMessage message)
        {
            ActivateItem(IoC.Get<TimelinesViewModel>());
        }

        void IHandle<NavigateToUserMessage>.Handle(NavigateToUserMessage message)
        {
            _windowManager.ShowDialog(new ErrorViewModel("not done yet"));
        }

        public void Handle(NavigateToTopicMessage message)
        {
            _windowManager.ShowDialog(new ErrorViewModel("not done yet"));
        }
    }
}