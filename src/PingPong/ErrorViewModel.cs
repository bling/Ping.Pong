using Caliburn.Micro;

namespace PingPong
{
    public class ErrorViewModel : PropertyChangedBase
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