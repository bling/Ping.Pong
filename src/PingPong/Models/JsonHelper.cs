using System;
using System.Globalization;
using System.Json;
using Caliburn.Micro;

namespace PingPong.Models
{
    public static class JsonHelper
    {
        public static bool? GetBool(this JsonValue json, string key)
        {
            var value = json[key];
            if (value != null)
                return value;

            return null;
        }

        public static DateTime GetSearchDateTime(this JsonValue json, string key)
        {
            return DateTime.Parse(json[key], CultureInfo.InvariantCulture);
        }

        public static DateTime GetDateTime(this JsonValue json, string key)
        {
            return DateTime.ParseExact(json[key],
                                       "ddd MMM d HH:mm:ss zzzzz yyyy", CultureInfo.InvariantCulture);
        }

        public static Tweet ToTweet(JsonObject value)
        {
            if (value.ContainsKey("text"))
                return Activate(() => new Tweet(value));

            return null;
        }

        public static SearchResult ToSearchResult(JsonValue value)
        {
            return Activate(() => new SearchResult(value));
        }

        public static DirectMessage ToDirectMessage(JsonObject value)
        {
            return Activate(() => new DirectMessage(value));
        }

        public static Relationship ToRelationship(JsonValue value)
        {
            return value.ContainsKey("relationship")
                       ? Activate(() => new Relationship(value["relationship"]))
                       : null;
        }

        public static User ToUser(JsonValue value)
        {
            return Activate(() => new User(value));
        }

        private static T Activate<T>(Func<T> activator) where T : class
        {
            try
            {
                return activator();
            }
            catch (Exception e)
            {
                LogManager.GetLog(typeof(JsonHelper)).Error(e);
                return null;
            }
        }
    }
}