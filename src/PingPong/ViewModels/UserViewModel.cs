﻿using System.Windows;
using Caliburn.Micro;
using PingPong.Core;
using PingPong.Models;

namespace PingPong.ViewModels
{
    public class UserViewModel : PropertyChangedBase
    {
        private readonly AppInfo _appInfo;
        private readonly TwitterClient _client;
        private ExtendedUser _user;
        private bool _canFollow;
        private bool _canUnfollow;

        public ExtendedUser User
        {
            get { return _user; }
            private set { this.SetValue("User", value, ref _user); }
        }

        public bool CanFollow
        {
            get { return _canFollow; }
            private set { this.SetValue("CanFollow", value, ref _canFollow); }
        }

        public bool CanUnfollow
        {
            get { return _canUnfollow; }
            private set { this.SetValue("CanUnfollow", value, ref _canUnfollow); }
        }

        public UserViewModel(AppInfo appInfo, TwitterClient client, string username)
        {
            _appInfo = appInfo;
            _client = client;

            Reload(username);
        }

        private void Reload(string username)
        {
            _client.GetUserInfo(username)
                .DispatcherSubscribe(x =>
                {
                    User = new ExtendedUser(x);
                    _client.GetRelationship(_appInfo.User.ScreenName, username)
                        .DispatcherSubscribe(r =>
                        {
                            User.Following = r.Source.IsFollowing;
                            User.FollowsBack = r.Source.IsFollowedBy;

                            CanFollow = !User.Following;
                            CanUnfollow = User.Following;
                        });
                });
        }

        public void Follow(User user)
        {
            _client.Follow(user.ScreenName);
            Reload(user.ScreenName);
        }

        public void Unfollow(User user)
        {
            if (MessageBox.Show(string.Format("Are you sure you want to unfollow {0}?", user.ScreenName), "", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                _client.Unfollow(user.ScreenName);
                Reload(user.ScreenName);
            }
        }
    }
}