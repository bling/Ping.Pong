using System.ComponentModel;

namespace PingPong.Models
{
    public class ExtendedUser : User, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private bool _followsBack;

        public ExtendedUser(User user)
        {
            this.CopyProperties(user);
        }

        public bool FollowsBack
        {
            get { return _followsBack; }
            set
            {
                _followsBack = value;
                PropertyChanged(this, new PropertyChangedEventArgs("FollowsBack"));
            }
        }
    }
}