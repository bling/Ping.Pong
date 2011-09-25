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

        public static DateTime GetDateTime(this JsonValue json, string key)
        {
            return DateTime.ParseExact(json[key],
                                       "ddd MMM d HH:mm:ss zzzzz yyyy", CultureInfo.InvariantCulture);
        }

        public static Tweet ToTweet(JsonObject value)
        {
            try
            {
                if (value.ContainsKey("text") && value.ContainsKey("user"))
                    return new Tweet(value);
            }
            catch (Exception e)
            {
                LogManager.GetLog(typeof(JsonHelper)).Error(e);
            }
            return null;
        }

        public static DirectMessage ToDirectMessage(JsonObject value)
        {
            try
            {
                return new DirectMessage(value);
            }
            catch (Exception e)
            {
                LogManager.GetLog(typeof(JsonHelper)).Error(e);
                return null;
            }
        }
    }
}