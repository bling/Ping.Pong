using System;
using System.Json;
using System.Linq;
using Caliburn.Micro;

namespace PingPong.Models
{
    public class Tweet : PropertyChangedBase
    {
        public Tweet(JsonObject json)
        {
            Id = json["id"];
            Text = json["text"]; // explicit conversion will unescape json
            Text = Text.UnescapeXml(); // unescape again for & escapes
            Source = json["source"];
            IsRetweet = json["retweeted"];
            InReplyToStatusId = json["in_reply_to_status_id_str"];
            InReplyToScreenName = json["in_reply_to_screen_name"];
            CreatedAt = json.GetDateTime("created_at");

            JsonValue entities;
            if (json.TryGetValue("entities", out entities))
                Entities = new Entities(entities);

            User = new User(json["user"]);
        }

        public ulong Id { get; private set; }

        public string Text { get; private set; }

        public User User { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public string Source { get; private set; }

        public bool IsRetweet { get; private set; }

        public string InReplyToStatusId { get; private set; }

        public string InReplyToScreenName { get; private set; }

        public Entities Entities { get; private set; }
    }

    public class Entities
    {
        public Entities(JsonValue json)
        {
            Urls = ((JsonArray)json["urls"]).Select(x => new UrlInfo(x)).ToArray();
            UserMentions = ((JsonArray)json["user_mentions"]).Select(x => new UserMention(x)).ToArray();
            Hashtags = ((JsonArray)json["hashtags"]).Select(x => new Hashtag(x)).ToArray();
        }

        public UrlInfo[] Urls { get; private set; }
        public UserMention[] UserMentions { get; private set; }
        public Hashtag[] Hashtags { get; private set; }
    }

    public class UrlInfo
    {
        public UrlInfo(JsonValue json)
        {
            ExpandedUrl = json["expanded_url"];
            Url = json["url"];
            Indices = ((JsonArray)json["indices"]).Select(x => (int)x).ToArray();
        }

        public string ExpandedUrl { get; private set; }
        public string Url { get; private set; }
        public int[] Indices { get; private set; }
    }

    public class UserMention
    {
        public UserMention(JsonValue json)
        {
            Id = json["id"];
            Name = json["name"];
            ScreenName = json["screen_name"];
            Indices = ((JsonArray)json["indices"]).Select(x => (int)x).ToArray();
        }

        public string Name { get; private set; }
        public string ScreenName { get; private set; }
        public ulong Id { get; private set; }
        public int[] Indices { get; private set; }
    }

    public class Hashtag
    {
        public Hashtag(JsonValue json)
        {
            Text = json["text"];
            Indices = ((JsonArray)json["indices"]).Select(x => (int)x).ToArray();
        }

        public string Text { get; private set; }
        public int[] Indices { get; private set; }
    }
}