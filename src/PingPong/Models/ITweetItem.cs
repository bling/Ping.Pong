using System;

namespace PingPong.Models
{
    public interface ITweetItem
    {
        ulong Id { get; }
        User User { get; }
        DateTime CreatedAt { get; }
        string Text { get; }
    }
}