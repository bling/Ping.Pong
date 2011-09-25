using System.Collections.ObjectModel;

namespace PingPong.Models
{
    public class TweetCollection : ObservableCollection<Tweet>
    {
        protected const int MaxTweets = 1000;

        public void Append(Tweet tweet)
        {
            while (Count > MaxTweets)
                RemoveAt(Count - 1);

            Tweet first = Count > 0 ? this[0] : null;
            if (first != null)
            {
                if (tweet.Id > first.Id)
                {
                    Insert(0, tweet);
                    return;
                }
            }
            Add(tweet);
        }
    }
}