using System.Diagnostics;
using PingPong.Core;

namespace PingPong.OAuth
{
    [DebuggerDisplay("Key = {Key}, Secret = {Secret}")]
    public abstract class Token
    {
        public string Key { get; private set; }
        public string Secret { get; private set; }

        protected Token(string key, string secret)
        {
            Enforce.NotNull(key, "key");
            Enforce.NotNull(secret, "secret");

            Key = key;
            Secret = secret;
        }
    }

    public class AccessToken : Token
    {
        public AccessToken(string key, string secret)
            : base(key, secret)
        {
        }
    }

    public class RequestToken : Token
    {
        public RequestToken(string key, string secret)
            : base(key, secret)
        {
        }
    }
}