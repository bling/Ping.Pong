using System;
using Caliburn.Micro;

namespace PingPong.Models
{
    public interface ITweetItem : INotifyPropertyChangedEx
    {
        ulong Id { get; }
        User User { get; }
        DateTime CreatedAt { get; }
        string Text { get; }
    }
}