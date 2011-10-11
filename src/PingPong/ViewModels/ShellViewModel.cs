using System;
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
        private readonly Func<TimelinesViewModel> _timelinesViewModelFactory;
        private readonly Func<AuthorizationViewModel> _authorizationViewModelFactory;
        private readonly Func<InstallViewModel> _installViewModelFactory;

        public ShellViewModel(AppInfo appInfo,
                              TwitterClient client,
                              Func<TimelinesViewModel> timelinesViewModelFactory,
                              Func<AuthorizationViewModel> authorizationViewModelFactory,
                              Func<InstallViewModel> installViewModelFactory)
        {
            _appInfo = appInfo;
            _client = client;
            _timelinesViewModelFactory = timelinesViewModelFactory;
            _authorizationViewModelFactory = authorizationViewModelFactory;
            _installViewModelFactory = installViewModelFactory;
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
                ActivateItem(_installViewModelFactory());
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
                                ActivateItem(_timelinesViewModelFactory());
                            });
                    }
                    else
                    {
                        ActivateItem(_authorizationViewModelFactory());
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
            ActivateItem(_timelinesViewModelFactory());
        }
    }
}