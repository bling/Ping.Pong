using PingPong.Models;

namespace PingPong
{
    public interface ITimelineNavigator
    {
        void NavigateToTopicMessage(string topic);
        void NavigateToUserTimeline(string screenName);
        void NavigateToConversationTimeline(ITweetItem item);
    }
}