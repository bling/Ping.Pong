using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using PingPong.Models;

namespace PingPong.Behaviors
{
    public class RefreshDateBehavior : Behavior<ItemsControl>
    {
        private IDisposable _subscription;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Unloaded += OnUnloaded;
            _subscription = Observable.Interval(TimeSpan.FromSeconds(20))
                .DispatcherSubscribe(_ =>
                {
                    foreach (var tweet in AssociatedObject.Items.OfType<ITweetItem>())
                        tweet.NotifyOfPropertyChange("CreatedAt");
                });
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Unloaded -= OnUnloaded;
            _subscription.Dispose();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            _subscription.Dispose();
        }
    }
}