using System.Json;

namespace PingPong.Models
{
    public static class JsonHelper
    {
        public static bool? GetValue(this JsonValue json, string key)
        {
            var value = json[key];
            if (value != null)
                return value;

            return null;
        }
    }
}