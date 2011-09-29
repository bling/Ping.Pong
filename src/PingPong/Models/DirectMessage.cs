using System;
using System.Json;
using Caliburn.Micro;

namespace PingPong.Models
{
    public class DirectMessage : PropertyChangedBase, ITweetItem
    {
        public DirectMessage(JsonObject json)
        {
            Id = json["id"];
            CreatedAt = json.GetDateTime("created_at");
            Text = json["text"];
            Sender = new User(json["sender"]);
            Recipient = new User(json["recipient"]);
        }

        public ulong Id { get; set; }
        public User Sender { get; set; }
        public User Recipient { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Text { get; set; }

        public User User
        {
            get { return Sender; }
        }
    }
}