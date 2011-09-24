using System;
using System.Globalization;
using System.Json;
using System.Linq;
using Caliburn.Micro;

namespace PingPong.Models
{
    public class Tweet : PropertyChangedBase
    {
        public static Tweet TryParse(JsonValue value)
        {
            if (value is JsonObject && value.ContainsKey("text") && value.ContainsKey("user"))
            {
                try
                {
                    return new Tweet((JsonObject)value);
                }
                catch (Exception e)
                {
                    LogManager.GetLog(typeof(Tweet)).Error(e);
                }
            }
            return null;
        }

        public Tweet(JsonObject json)
        {
            User = new User(json["user"]);
            Id = json["id"];
            Text = json["text"]; // explicit conversion will unescape json
            Text = Text.UnescapeXml(); // unescape again for & escapes
            Source = json["source"];
            IsRetweet = json["retweeted"];
            InReplyToStatusId = json["in_reply_to_status_id_str"];
            InReplyToScreenName = json["in_reply_to_screen_name"];
            CreatedAt = DateTime.ParseExact(json["created_at"],
                                            "ddd MMM d HH:mm:ss zzzzz yyyy", CultureInfo.InvariantCulture);

            JsonValue entities;
            if (json.TryGetValue("entities", out entities))
                Entities = new Entities(entities);
        }

        public ulong Id { get; set; }

        public string Text { get; set; }

        public User User { get; set; }

        public DateTime CreatedAt { get; set; }

        public string Source { get; set; }

        public bool IsRetweet { get; set; }

        public string InReplyToStatusId { get; set; }

        public string InReplyToScreenName { get; set; }

        public Entities Entities { get; set; }
    }

    public class Entities
    {
        public Entities(JsonValue json)
        {
            Urls = ((JsonArray)json["urls"]).Select(x => new UrlInfo(x)).ToArray();
            UserMentions = ((JsonArray)json["user_mentions"]).Select(x => new UserMention(x)).ToArray();
            Hashtags = ((JsonArray)json["hashtags"]).Select(x => new Hashtag(x)).ToArray();
        }

        public UrlInfo[] Urls { get; set; }
        public UserMention[] UserMentions { get; set; }
        public Hashtag[] Hashtags { get; set; }
    }

    public class UrlInfo
    {
        public UrlInfo(JsonValue json)
        {
            ExpandedUrl = json["expanded_url"];
            Url = json["url"];
            Indices = ((JsonArray)json["indices"]).Select(x => (int)x).ToArray();
        }

        public string ExpandedUrl { get; set; }
        public string Url { get; set; }
        public int[] Indices { get; set; }
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

        public string Name { get; set; }
        public string ScreenName { get; set; }
        public ulong Id { get; set; }
        public int[] Indices { get; set; }
    }

    public class Hashtag
    {
        public Hashtag(JsonValue json)
        {
            Text = json["text"];
            Indices = ((JsonArray)json["indices"]).Select(x => (int)x).ToArray();
        }

        public string Text { get; set; }
        public int[] Indices { get; set; }
    }
}