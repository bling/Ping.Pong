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
        private readonly IDisposable _subscription;
        private readonly NotificationWindow _window;

        public NotificationService(TwitterClient client)
        {
            _window = new NotificationWindow
            {
                Width = 400,
                Height = 100,
                Content = new NotificationControl()
            };

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
            _window.Content.DataContext = tweet;
            _window.Close();
            _window.Show(5000);
        }
    }
}