using System.Windows;
using Caliburn.Micro;
using PingPong.Models;

namespace PingPong
{
    public class AppInfo : PropertyChangedBase
    {
        private User _user;
        private Style _statusStyle;
        private bool _isNotificationsEnabled;

        public User User
        {
            get { return _user; }
            set { this.SetValue("User", value, ref _user); }
        }

        public Style StatusStyle
        {
            get { return _statusStyle; }
            set { this.SetValue("StatusStyle", value, ref _statusStyle); }
        }

        public bool IsNotificationsEnabled
        {
            get { return _isNotificationsEnabled; }
            set
            {
                this.SetValue("IsNotificationsEnabled", value, ref _isNotificationsEnabled);
                AppSettings.IsNotificationsEnabled = value;
            }
        }

        public AppInfo()
        {
            IsNotificationsEnabled = AppSettings.IsNotificationsEnabled;
            StatusStyle = (Style)Application.Current.Resources["DarkTweetsPanel"];
        }
    }
}