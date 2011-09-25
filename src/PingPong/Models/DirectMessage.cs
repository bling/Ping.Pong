using System;
using System.Json;

namespace PingPong.Models
{
    public class DirectMessage
    {
        public DirectMessage(JsonObject json)
        {
            Id = json["id"];
            CreatedAt = json.GetDateTime("created_at");
            Text = json["text"];
            Sender = new User(json["sender"]);
            Recipient = new User(json["recipient"]);
        }

        public ulong Id { get; private set; }
        public User Sender { get; private set; }
        public User Recipient { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public string Text { get; private set; }
    }
}