using PingPong.Core;

namespace PingPong.Messages
{
    public class NavigateToUserMessage
    {
        public string User { get; private set; }

        public NavigateToUserMessage(string user)
        {
            User = user.Trim(TweetParser.PunctuationChars).Trim('@');
        }
    }

    public class NavigateToTopicMessage
    {
        public string Topic { get; private set; }

        public NavigateToTopicMessage(string topic)
        {
            Topic = topic.Trim(TweetParser.PunctuationChars).Trim('@', '#');
        }
    }

    public class ShowTimelinesMessage
    {
    }
}