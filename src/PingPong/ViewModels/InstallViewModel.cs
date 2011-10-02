using System.Windows;
using Caliburn.Micro;

namespace PingPong.ViewModels
{
    public class InstallViewModel
    {
        private readonly IWindowManager _windowManager;

        public InstallViewModel(IWindowManager windowManager)
        {
            _windowManager = windowManager;
        }

        public void Install()
        {
            if (Application.Current.InstallState == InstallState.Installed)
            {
                _windowManager.ShowDialog(new ErrorViewModel("Already installed."));
            }
            else
            {
                Application.Current.Install();
            }
        }
    }
}