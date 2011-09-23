using System.Linq;
using PingPong.Core;

namespace PingPong.OAuth
{
    public class TokenResponse<T> where T : Token
    {
        public T Token { get; private set; }
        public ILookup<string, string> ExtraData { get; private set; }

        public TokenResponse(T token, ILookup<string, string> extraData)
        {
            Enforce.NotNull(token, "token");
            Enforce.NotNull(extraData, "extraData");

            Token = token;
            ExtraData = extraData;
        }
    }
}