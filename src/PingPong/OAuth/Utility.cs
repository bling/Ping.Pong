using System;
using System.Collections.Generic;
using System.Linq;

namespace PingPong.OAuth
{
    internal static class Utility
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long ToUnixTime(this DateTime target)
        {
            return (long)(target - UnixEpoch).TotalSeconds;
        }

        /// <summary>Escape RFC3986 String</summary>
        public static string UrlEncode(this string stringToEscape)
        {
            return Uri.EscapeDataString(stringToEscape)
                .Replace("!", "%21")
                .Replace("*", "%2A")
                .Replace("'", "%27")
                .Replace("(", "%28")
                .Replace(")", "%29");
        }

        public static string Wrap(this string input, string wrapper)
        {
            return wrapper + input + wrapper;
        }

        public static string ToString<T>(this IEnumerable<T> source, string separator)
        {
            return string.Join(separator, source.Select(x => x.ToString()));
        }
    }
}