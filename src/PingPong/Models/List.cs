using System;
using System.Json;

namespace PingPong.Models
{
    public class List
    {
        public List(JsonValue json)
        {
            Id = json["id_str"];
            Name = json["name"];
            FullName = json["full_name"];
            CreatedAt = json.GetDateTime("created_at");
            Uri = json["uri"];
            Description = json["description"];
            Slug = json["slug"];
            User = new User(json["user"]);
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string FullName { get; private set; }
        public DateTime CreatedAt { get; set; }
        public string Uri { get; set; }
        public string Description { get; set; }
        public string Slug { get; set; }
        public User User { get; set; }
    }
}