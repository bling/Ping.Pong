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
    }
}