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

            if (Count > 0)
            {
                for (int i = 0; i < Count; i++)
                {
                    if (tweet.CreatedAt > this[i].CreatedAt)
                    {
                        Insert(0, tweet);
                        return;
                    }
                }
            }
            Add(tweet);
        }
    }
}