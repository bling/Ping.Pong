using System;
using Caliburn.Micro;

namespace PingPong.Models
{
    public interface ITweetItem : INotifyPropertyChangedEx
    {
        string Id { get; }
        User User { get; }
        DateTime CreatedAt { get; }
        string Text { get; }
    }
}