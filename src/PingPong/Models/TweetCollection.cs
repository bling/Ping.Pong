using System.Collections.ObjectModel;

namespace PingPong.Models
{
    public class TweetCollection : ObservableCollection<ITweetItem>
    {
        protected const int MaxTweets = 1000;

        public void Append(ITweetItem tweet)
        {
            while (Count > MaxTweets)
                RemoveAt(Count - 1);

            ITweetItem first = Count > 0 ? this[0] : null;
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