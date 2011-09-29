using System;
using System.Json;
using Caliburn.Micro;

namespace PingPong.Models
{
    public class SearchResult : PropertyChangedBase, ITweetItem
    {
        public SearchResult(JsonValue json)
        {
            Text = json["text"]; // explicit conversion will unescape json
            Text = Text.UnescapeXml(); // unescape again for & escapes
            Source = json["source"];
            CreatedAt = json.GetSearchDateTime("created_at");
            User = new User
            {
                Id = json["from_user_id"],
                ScreenName = json["from_user"],
                ProfileImageUrl = json["profile_image_url"]
            };
        }

        public ulong Id { get; set; }
        public User User { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Text { get; set; }
        public string Source { get; set; }
    }
}