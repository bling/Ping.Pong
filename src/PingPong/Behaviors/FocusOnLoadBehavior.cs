using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace PingPong.Behaviors
{
    public class FocusOnLoadBehavior : Behavior<TextBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Loaded -= OnLoaded;
            AssociatedObject.Focus();
            AssociatedObject.SelectionStart = AssociatedObject.Text.Length;
        }
    }
}