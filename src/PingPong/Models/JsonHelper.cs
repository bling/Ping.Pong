using System;
using System.Globalization;
using System.Json;

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
    }
}