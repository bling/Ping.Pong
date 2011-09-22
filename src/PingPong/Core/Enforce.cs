using System;

namespace PingPong.Core
{
    public static class Enforce
    {
        public static void NotNull<T>(T value) where T : class
        {
            if (value == null)
                throw new ArgumentNullException();
        }

        public static void NotNullOrEmpty(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException();
        }
    }
}