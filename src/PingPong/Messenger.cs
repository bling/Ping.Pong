using System.Linq;
using Caliburn.Micro;
using PingPong.Core;
using PingPong.Messages;
using PingPong.Models;

namespace PingPong
{
    public class Messenger
    {
        private readonly TweetParser _tweetParser;
        private readonly IEventAggregator _eventAggregator;

        public Messenger(TweetParser tweetParser, IEventAggregator eventAggregator)
        {
            _tweetParser = tweetParser;
            _eventAggregator = eventAggregator;
        }

        public void NavigateToUserTimeline(string screenName)
        {
            _eventAggregator.Publish(new NavigateToUserMessage(screenName));
        }

        public void NavigateToConversationTimeline(ITweetItem item)
        {
            if (item is Tweet)
            {
                var tweet = (Tweet)item;
                string other;
                if (tweet.Entities != null)
                {
                    other = tweet.Entities.UserMentions[0].ScreenName;
                }
                else
                {
                    int length;
                    var parts = _tweetParser.Parse(tweet.Text, out length);
                    other = parts.First(x => x.Type == TweetPartType.User).Text.Trim('@');
                }

                _eventAggregator.Publish(new NavigateToConversationMessage(item.User.ScreenName, other));
            }
        }
    }
}