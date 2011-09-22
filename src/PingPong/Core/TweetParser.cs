using System;
using System.Collections.Generic;

namespace PingPong.Core
{
    public class TweetParser
    {
        public const int MaximumTcoLinkLength = 20;
        public const int MaxLength = 140;

        private readonly char[] PunctuationChars = new[]
        {
            '.', '?', '!'
        };

        public IList<TweetPart> Parse(string text, out int characters)
        {
            var result = new List<TweetPart>();
            var parts = text.Split(' ');
            characters = 0;
            foreach (var p in parts)
            {
                if (p.StartsWith("#"))
                {
                    characters += p.Length;
                    result.Add(new TweetPart(TweetPartType.Topic, p));
                }
                else if (p.StartsWith("http://") || p.StartsWith("https://"))
                {
                    characters += Math.Min(p.Length, MaximumTcoLinkLength);
                    string cleanLink = p.TrimEnd(PunctuationChars);
                    Uri uri;
                    result.Add(Uri.TryCreate(cleanLink, UriKind.RelativeOrAbsolute, out uri)
                                   ? new TweetPart(TweetPartType.Hyperlink, p, uri)
                                   : new TweetPart(TweetPartType.Text, p));
                }
                else if (p.StartsWith("@"))
                {
                    characters += p.Length;
                    result.Add(new TweetPart(TweetPartType.User, p));
                }
                else
                {
                    characters += p.Length;
                    result.Add(new TweetPart(TweetPartType.Text, p));
                }
                characters++; // for space
            }
            characters--; // last space was extra
            return result;
        }
    }

    public enum TweetPartType
    {
        Topic,
        Hyperlink,
        Text,
        User
    }

    public class TweetPart
    {
        public TweetPartType Type { get; private set; }
        public string Text { get; private set; }
        public object State { get; private set; }

        public TweetPart(TweetPartType type, string text, object state = null)
        {
            Type = type;
            Text = text;
            State = state;
        }
    }
}