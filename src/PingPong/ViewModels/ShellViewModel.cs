using System.Windows;
using Caliburn.Micro;
using PingPong.Core;
using PingPong.Messages;

namespace PingPong.ViewModels
{
    public class ShellViewModel : Conductor<IScreen>.Collection.OneActive,
                                  IShell,
                                  IHandle<ShowTimelinesMessage>
    {
        private readonly AppInfo _appInfo;
        private readonly TwitterClient _client;
        private readonly ViewModelFactory _viewModelFactory;

        public ShellViewModel(AppInfo appInfo,
                              TwitterClient client,
                              ViewModelFactory viewModelFactory)
        {
            _appInfo = appInfo;
            _client = client;
            _viewModelFactory = viewModelFactory;
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            if (Application.Current.IsRunningOutOfBrowser)
            {
                Application.Current.CheckAndDownloadUpdateCompleted += OnCheckAndDownloadUpdateCompleted;
                Application.Current.CheckAndDownloadUpdateAsync();
            }
            else
            {
                ActivateItem(_viewModelFactory.InstallFactory());
            }
        }

        private void OnCheckAndDownloadUpdateCompleted(object sender, CheckAndDownloadUpdateCompletedEventArgs e)
        {
            if (e.UpdateAvailable)
            {
                ActivateItem(new ErrorViewModel("ping.pong has been updated to a newer version...please restart."));
            }
            else
            {
                if (string.IsNullOrEmpty(AppBootstrapper.ConsumerKey) || string.IsNullOrEmpty(AppBootstrapper.ConsumerSecret))
                {
                    ActivateItem(new ErrorViewModel("Please create your own consumer key/secret from Twitter."));
                }
                else
                {
                    if (AppSettings.HasAuthToken)
                    {
                        _client.GetAccountVerification()
                            .DispatcherSubscribe(x =>
                            {
                                _appInfo.User = x;
                                ActivateItem(_viewModelFactory.TimelinesFactory());
                            });
                    }
                    else
                    {
                        ActivateItem(_viewModelFactory.AuthorizationFactory());
                    }
                }
            }
        }

        public void DragMove()
        {
            Application.Current.MainWindow.DragMove();
        }

        public void Minimize()
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        public void Maximize()
        {
            if (Application.Current.MainWindow.WindowState == WindowState.Normal)
                Application.Current.MainWindow.WindowState = WindowState.Maximized;
            else if (Application.Current.MainWindow.WindowState == WindowState.Maximized)
                Application.Current.MainWindow.WindowState = WindowState.Normal;
        }

        public void Close()
        {
            Application.Current.MainWindow.Close();
        }

        void IHandle<ShowTimelinesMessage>.Handle(ShowTimelinesMessage message)
        {
            ActivateItem(_viewModelFactory.TimelinesFactory());
        }
    }
}