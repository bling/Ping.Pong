using PingPong.Models;

namespace PingPong
{
    public class UserInfoViewModel
    {
        public User Account { get; private set; }

        public UserInfoViewModel(User account)
        {
            Account = account;
        }
    }
}