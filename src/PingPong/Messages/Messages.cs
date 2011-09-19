using PingPong.Models;

namespace PingPong.Messages
{
    public class NavigateToUserMessage
    {
        public string User { get; private set; }

        public NavigateToUserMessage(string user)
        {
            User = user;
        }
    }

    public class NavigateToTopicMessage
    {
        public string Topic { get; private set; }

        public NavigateToTopicMessage(string topic)
        {
            Topic = topic;
        }
    }

    public class ShowTimelinesMessage
    {
    }

    public class RetweetMessage
    {
        public Tweet Tweet { get; private set; }

        public RetweetMessage(Tweet tweet)
        {
            Tweet = tweet;
        }
    }

    public class ReplyMessage
    {
        public Tweet Tweet { get; private set; }

        public ReplyMessage(Tweet tweet)
        {
            Tweet = tweet;
        }
    }

    public class QuoteMessage
    {
        public Tweet Tweet { get; private set; }

        public QuoteMessage(Tweet tweet)
        {
            Tweet = tweet;
        }
    }
}