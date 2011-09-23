using System;

namespace PingPong.Core
{
    public static class Enforce
    {
        public static void NotNull<T>(T value, string argument = null) where T : class
        {
            if (value == null)
                throw new ArgumentNullException(argument);
        }

        public static void NotNullOrEmpty(string value, string argument = null)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException(argument);
        }
    }
}