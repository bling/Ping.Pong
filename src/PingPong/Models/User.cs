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
            IsFollowing = json.GetBool("following");
        }

        public ulong Id { get; private set; }
        public string Name { get; private set; }
        public string ScreenName { get; private set; }
        public string ProfileImageUrl { get; private set; }
        public bool? IsFollowing { get; private set; }
    }
}