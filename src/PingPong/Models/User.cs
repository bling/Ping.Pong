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

        public User()
        {
        }

        public ulong Id { get; set; }
        public string Name { get; set; }
        public string ScreenName { get; set; }
        public string ProfileImageUrl { get; set; }
        public string Location { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public int Friends { get; set; }
        public int Followers { get; set; }
        public int Statuses { get; set; }
        public bool? IsFollowing { get; set; }
    }
}