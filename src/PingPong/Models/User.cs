using System.Json;

namespace PingPong.Models
{
    public class User
    {
        public User(JsonValue json)
        {
            Id = json["id"];
            Name = json["name"];
            ScreenName = json["screen_name"];
            ProfileImageUrl = json["profile_image_url"];
            Location = json["location"];
            Url = json["url"];
            Description = json["description"];
            Friends = json["friends_count"];
            Followers = json["followers_count"];
            Statuses = json["statuses_count"];
            IsFollowing = json.GetBool("following");
        }

        public ulong Id { get; private set; }
        public string Name { get; private set; }
        public string ScreenName { get; private set; }
        public string ProfileImageUrl { get; private set; }
        public string Location { get; private set; }
        public string Url { get; private set; }
        public string Description { get; private set; }
        public int Friends { get; private set; }
        public int Followers { get; private set; }
        public int Statuses { get; private set; }
        public bool? IsFollowing { get; private set; }
    }
}