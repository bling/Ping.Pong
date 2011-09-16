using Caliburn.Micro;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Json;

namespace PingPong.Models
{
    public class Tweet : PropertyChangedBase
    {
        public static Tweet TryParse(JsonValue value)
        {
            if (value is JsonObject && value.ContainsKey("text") && value.ContainsKey("user"))
                return new Tweet((JsonObject)value);

            return null;
        }

        public Tweet()
        {
        }

        private Tweet(JsonObject json)
        {
            Id = json["id"].ToString();
            Text = json["text"]; // explicit conversion will unescape json
            Text = Text.UnescapeXml(); // unescape again for & escapes
            ScreenName = json["user"]["screen_name"];
            ProfileImgUrl = json["user"]["profile_image_url"];
            Source = json["source"];
            IsRetweet = json["retweeted"];
            InReplyToStatusId = json["in_reply_to_status_id_str"];
            InReplyToScreenName = json["in_reply_to_screen_name"];
            CreatedAt = DateTime.ParseExact(json["created_at"],
                                            "ddd MMM d HH:mm:ss zzzzz yyyy", CultureInfo.InvariantCulture);
        }

        public string Id { get; set; }

        public string Text { get; set; }

        public string ScreenName { get; set; }

        public DateTime CreatedAt { get; set; }

        public string ProfileImgUrl { get; set; }

        public string Source { get; set; }

        public bool IsRetweet { get; set; }

        public string InReplyToStatusId { get; set; }

        public string InReplyToScreenName { get; set; }
    }

    public class TweetCollection : ObservableCollection<Tweet>
    {
    }
}