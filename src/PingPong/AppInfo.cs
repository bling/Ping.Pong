using System.Windows;
using Caliburn.Micro;
using PingPong.Models;

namespace PingPong
{
    public class AppInfo : PropertyChangedBase
    {
        private User _user;

        public User User
        {
            get { return _user; }
            set { this.SetValue("User", value, ref _user); }
        }

        private Style _statusStyle;

        public Style StatusStyle
        {
            get { return _statusStyle; }
            set { this.SetValue("StatusStyle", value, ref _statusStyle); }
        }

        public AppInfo()
        {
            StatusStyle = (Style)Application.Current.Resources["DarkTweetsPanel"];
        }
    }
}