using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace PingPong.Behaviors
{
    public class AutoSizeBehavior : Behavior<ItemsControl>
    {
        public static readonly DependencyProperty ItemsControlProperty =
            DependencyProperty.Register("ItemsControl",
                                        typeof(ItemsControl),
                                        typeof(AutoSizeBehavior),
                                        new PropertyMetadata(OnItemsSourceChanged));

        public ItemsControl ItemsControl
        {
            get { return (ItemsControl)GetValue(ItemsControlProperty); }
            set { SetValue(ItemsControlProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            OnItemsControlSizeChanged(null, null);
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (AutoSizeBehavior)d;
            behavior.ItemsControl.SizeChanged -= behavior.OnItemsControlSizeChanged;
            behavior.ItemsControl.SizeChanged += behavior.OnItemsControlSizeChanged;

            var collection = behavior.ItemsControl.ItemsSource as INotifyCollectionChanged;
            if (collection != null)
                collection.CollectionChanged += behavior.OnItemsControlSizeChanged;
        }

        private void OnItemsControlSizeChanged(object sender, EventArgs e)
        {
            if (ItemsControl.Items.Count > 0)
                AssociatedObject.Width = ItemsControl.ActualWidth / ItemsControl.Items.Count;
        }
    }
}