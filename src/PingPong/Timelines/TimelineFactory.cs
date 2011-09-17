using System;
using System.Reactive.Subjects;
using Autofac.Features.OwnedInstances;
using PingPong.Models;

namespace PingPong.Timelines
{
    public class TimelineFactory
    {
        public Func<StatusType, Owned<StatusTimeline>> StatusFactory { get; set; }
        public Func<Owned<DirectMessageTimeline>> DirectMessageFactory { get; set; }
        public Func<IConnectableObservable<Tweet>, Owned<StreamingTimeline>> StreamingFactory { get; set; }
    }

    public enum StatusType
    {
        Home,
        Mentions,
    }
}