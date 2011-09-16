using System.Windows;

namespace PingPong
{
    public class InstallViewModel
    {
        public void Install()
        {
            Application.Current.Install();
        }
    }
}