using System.Diagnostics;
using Caliburn.Micro;
using PingPong.Core;

namespace PingPong.ViewModels
{
    public class TestViewModel : Screen
    {
        private readonly TwitterClient _client;

        public TestViewModel()
        {
            _client = new TwitterClient();
        }

        public void Homeline()
        {
            _client.GetHomeTimeline()
                   .DispatcherSubscribe(t => Debug.WriteLine(t));
        }

        public void Mentions()
        {
            _client.GetMentions()
                   .DispatcherSubscribe(t => Debug.WriteLine(t));
        }

        public void RateLimit()
        {
            _client.GetRateLimitStatus()
                   .DispatcherSubscribe(rl => Debug.WriteLine(rl));
        }
    }
}