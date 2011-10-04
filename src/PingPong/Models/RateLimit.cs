using System;
using System.Json;

namespace PingPong.Models
{
    public class RateLimit
    {
        public RateLimit(JsonValue json)
        {
            RemainingHits = json["remaining_hits"];
            ResetTime = json.GetDateTime("reset_time");
            HourlyLimit = json["hourly_limit"];
        }

        public int RemainingHits { get; set; }
        public DateTime ResetTime { get; set; }
        public int HourlyLimit { get; set; }
    }
}