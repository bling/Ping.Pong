using System;
using System.Linq;
using System.Reactive.Linq;
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
            _subscription = Observable.Interval(TimeSpan.FromSeconds(20))
                .DispatcherSubscribe(_ =>
                {
                    foreach (var tweet in AssociatedObject.Items.Cast<ITweetItem>())
                        tweet.NotifyOfPropertyChange("CreatedAt");
                });
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            _subscription.Dispose();
        }
    }
}