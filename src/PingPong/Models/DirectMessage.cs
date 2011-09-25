using System;
using System.Json;
using Caliburn.Micro;

namespace PingPong.Models
{
    public class DirectMessage
    {
        public static DirectMessage TryParse(JsonValue json)
        {
            try
            {
                return new DirectMessage(json);
            }
            catch (Exception e)
            {
                LogManager.GetLog(typeof(DirectMessage)).Error(e);
            }
        }

        public DirectMessage(JsonValue json)
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