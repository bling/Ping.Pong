using System;
using Autofac.Features.OwnedInstances;

namespace PingPong.Timelines
{
    public class TimelineFactory
    {
        public Func<StatusType, Owned<StatusTimeline>> StatusFactory { get; set; }
        public Func<Owned<DirectMessageTimeline>> DirectMessageFactory { get; set; }
        public Func<Owned<StreamingTimeline>> StreamingFactory { get; set; }
    }

    public enum StatusType
    {
        Home,
        Mentions,
    }
}