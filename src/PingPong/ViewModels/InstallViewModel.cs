using System.Windows;
using Caliburn.Micro;

namespace PingPong.ViewModels
{
    public class InstallViewModel : Screen
    {
        private string _text;

        public bool IsInstalled
        {
            get { return Application.Current.InstallState == InstallState.Installed; }
        }

        public bool CanInstall
        {
            get { return !IsInstalled; }
        }

        public string Text
        {
            get { return _text; }
            set { this.SetValue("Text", value, ref _text); }
        }

        public InstallViewModel()
        {
            Text = IsInstalled ? "already installed" : "install to desktop";
        }

        public void Install()
        {
            Application.Current.Install();
        }
    }
}