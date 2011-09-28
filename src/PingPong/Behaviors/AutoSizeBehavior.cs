namespace PingPong.Behaviors
{
    using System;
    using System.Collections.Specialized;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interactivity;
    using System.Windows.Media;
    using PingPong.Core;

    public class AutoSizeBehavior : Behavior<ItemsControl>
    {
        private ItemsControl _itemsControl;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += delegate
            {
                ItemsControl ic;
                DependencyObject current = AssociatedObject;
                DependencyObject parent;
                do
                {
                    parent = VisualTreeHelper.GetParent(current);
                    ic = parent as ItemsControl;
                    current = parent;
                } while (parent != null && ic == null);

                AttachTo(ic);
                OnItemsControlSizeChanged(null, null);
            };
        }

        private void AttachTo(ItemsControl itemsControl)
        {
            Enforce.NotNull(itemsControl);

            _itemsControl = itemsControl;
            _itemsControl.SizeChanged -= OnItemsControlSizeChanged;
            _itemsControl.SizeChanged += OnItemsControlSizeChanged;

            var collection = _itemsControl.ItemsSource as INotifyCollectionChanged;
            if (collection != null)
                collection.CollectionChanged += OnItemsControlSizeChanged;
        }

        private void OnItemsControlSizeChanged(object sender, EventArgs e)
        {
            if (_itemsControl.Items.Count > 0)
                this.AssociatedObject.Width = _itemsControl.ActualWidth / _itemsControl.Items.Count;
        }
    }
}