using System.Windows;
using Autofac;
using Caliburn.Micro;
using PingPong.Messages;
using PingPong.Models;

namespace PingPong
{
    public class ShellViewModel : Conductor<object>,
                                  IShell,
                                  IHandle<ShowTimelinesMessage>
    {
        private readonly IContainer _container;

        public ShellViewModel(IContainer container)
        {
            _container = container;
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
                                 ? (object)_container.Resolve<TimelinesViewModel>()
                                 : _container.Resolve<AuthorizationViewModel>());
            }
            else
            {
                ActivateItem(_container.Resolve<InstallViewModel>());
            }
        }

        void IHandle<ShowTimelinesMessage>.Handle(ShowTimelinesMessage message)
        {
            ActivateItem(_container.Resolve<TimelinesViewModel>());
        }

        public void Reply(Tweet tweet)
        {
            ((TimelinesViewModel)ActiveItem).ReplyTo(tweet);
        }

        public void Retweet(Tweet tweet)
        {
            ((TimelinesViewModel)ActiveItem).Retweet(tweet);
        }

        public void Quote(Tweet tweet)
        {
            ((TimelinesViewModel)ActiveItem).Quote(tweet);
        }

        public void DirectMessage(Tweet tweet)
        {
            ((TimelinesViewModel)ActiveItem).DirectMessage(tweet);
        }
    }
}