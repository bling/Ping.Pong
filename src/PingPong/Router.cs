using Caliburn.Micro;
using PingPong.Messages;
using PingPong.Models;

namespace PingPong
{
    public class Router
    {
        public void Reply(Tweet tweet)
        {
            IoC.Get<IEventAggregator>().Publish(new ReplyMessage(tweet));
        }

        public void Retweet(Tweet tweet)
        {
            IoC.Get<IEventAggregator>().Publish(new RetweetMessage(tweet));
        }

        public void Quote(Tweet tweet)
        {
            IoC.Get<IEventAggregator>().Publish(new QuoteMessage(tweet));
        }
    }
}