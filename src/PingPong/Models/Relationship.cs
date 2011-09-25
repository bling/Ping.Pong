using System.Json;

namespace PingPong.Models
{
    public class Relationship
    {
        public Relationship(JsonValue json)
        {
            Source = new RelationshipUser(json["source"]);
            Target = new RelationshipUser(json["target"]);
        }

        public RelationshipUser Source { get; private set; }
        public RelationshipUser Target { get; private set; }
    }

    public class RelationshipUser
    {
        public RelationshipUser(JsonValue json)
        {
            Id = json["id"];
            ScreenName = json["screen_name"];
            IsFollowing = json["following"];
            IsFollowedBy = json["followed_by"];
        }

        public ulong Id { get; private set; }
        public string ScreenName { get; private set; }
        public bool IsFollowing { get; private set; }
        public bool IsFollowedBy { get; private set; }
    }
}