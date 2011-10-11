using System;
using System.Reactive.Linq;
using System.Windows;
using PingPong.Controls;
using PingPong.Core;
using PingPong.Models;

namespace PingPong
{
    public class NotificationService : IDisposable
    {
        private readonly AppInfo _appInfo;
        private readonly IDisposable _subscription;

        public NotificationService(AppInfo appInfo, TwitterClient client)
        {
            _appInfo = appInfo;
            _subscription = client
                .Sample(TimeSpan.FromSeconds(6))
                .Where(t => (DateTime.Now - t.CreatedAt) < TimeSpan.FromSeconds(5))
                .DispatcherSubscribe(ShowWindow);
        }

        public void Dispose()
        {
            _subscription.Dispose();
        }

        private void ShowWindow(ITweetItem tweet)
        {
            if (_appInfo.IsNotificationsEnabled)
            {
                var nw = new NotificationWindow
                {
                    Width = 300,
                    Height = 100,
                    Content = new NotificationControl { DataContext = tweet }
                };
                nw.Show(5000);
            }
        }
    }
}