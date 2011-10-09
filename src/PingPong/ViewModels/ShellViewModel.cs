using System.Windows;
using Caliburn.Micro;
using PingPong.Messages;

namespace PingPong.ViewModels
{
    public class ShellViewModel : Conductor<object>,
                                  IShell,
                                  IHandle<ShowTimelinesMessage>
    {
        private readonly TimelinesViewModel _timelinesViewModel;
        private readonly AuthorizationViewModel _authorizationViewModel;
        private readonly ConfigurationViewModel _configurationViewModel;
        private readonly InstallViewModel _installViewModel;

        public ShellViewModel(TimelinesViewModel timelinesViewModel,
                              AuthorizationViewModel authorizationViewModel,
                              ConfigurationViewModel configurationViewModel,
                              InstallViewModel installViewModel)
        {
            _timelinesViewModel = timelinesViewModel;
            _authorizationViewModel = authorizationViewModel;
            _configurationViewModel = configurationViewModel;
            _installViewModel = installViewModel;
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
                ActivateItem(_installViewModel);
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
                    ActivateItem(AppSettings.HasAuthToken ? (object)_timelinesViewModel : _authorizationViewModel);
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

        public void Config()
        {
            ActivateItem(_configurationViewModel);
        }

        public void Timelines()
        {
            ActivateItem(_timelinesViewModel);
        }

        void IHandle<ShowTimelinesMessage>.Handle(ShowTimelinesMessage message)
        {
            ActivateItem(_timelinesViewModel);
        }
    }
}