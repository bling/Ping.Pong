using System;
using System.Json;

namespace PingPong.Models
{
    public class RateLimitStatus
    {
        public RateLimit HelpPrivacy { get; set; }
        public RateLimit HelpConfiguration { get; set; }
        public RateLimit StatusesOembed { get; set; }

        public RateLimitStatus(JsonValue json)
        {
            var resources = json["resources"];
            HelpConfiguration = new RateLimit(resources["help"]["/help/configuration"]);
            HelpPrivacy = new RateLimit(resources["help"]["/help/privacy"]);
            StatusesOembed = new RateLimit(resources["statuses"]["/statuses/oembed"]);
        }
    }

    public class RateLimit
    {
        public RateLimit(JsonValue json)
        {
            RemainingHits = json["remaining"];
            ResetTime = json.GetDateTime("reset");
            HourlyLimit = json["limit"];
        }

        public int RemainingHits { get; set; }
        public DateTime ResetTime { get; set; }
        public int HourlyLimit { get; set; }
    }
}