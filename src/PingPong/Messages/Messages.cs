using System.Linq;
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

    public class NavigateToConversationMessage
    {
        public string User1 { get; private set; }
        public string User2 { get; private set; }

        public NavigateToConversationMessage(string user1, string user2)
        {
            User1 = user1.Trim('@', '#');
            User2 = user2.Trim('@', '#');
        }
    }

    public class ShowTimelinesMessage
    {
    }
}