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

    public class ShowAuthorizationMessage
    {
    }
}