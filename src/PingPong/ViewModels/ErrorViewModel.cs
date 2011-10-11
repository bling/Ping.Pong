using Caliburn.Micro;

namespace PingPong.ViewModels
{
    public class ErrorViewModel : Screen
    {
        private string _text;

        public string Text
        {
            get { return _text; }
            set { this.SetValue("Text", value, ref _text); }
        }

        public ErrorViewModel(string message)
        {
            Text = message;
        }
    }
}