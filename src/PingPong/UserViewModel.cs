using System.Windows;
using Caliburn.Micro;
using PingPong.Core;
using PingPong.Models;

namespace PingPong
{
    public class UserViewModel : PropertyChangedBase
    {
        private readonly TwitterClient _client;

        public UserViewModel(TwitterClient client)
        {
            _client = client;
        }

        public void Follow(User user)
        {
            _client.Follow(user.ScreenName);
        }

        public void Unfollow(User user)
        {
            if (MessageBox.Show(string.Format("Are you sure you want to unfollow {0}?", user.ScreenName), "", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                _client.Unfollow(user.ScreenName);
        }
    }
}