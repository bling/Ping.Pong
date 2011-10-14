﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Text;

namespace PingPong.OAuth
{
    internal static class OAuthExtensions
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long ToUnixTime(this DateTime target)
        {
            return (long)(target - UnixEpoch).TotalSeconds;
        }

        /// <summary>Escape RFC3986 string.</summary>
        public static string UrlEncode(this string stringToEscape)
        {
            return Uri.EscapeDataString(stringToEscape)
                .Replace("!", "%21")
                .Replace("*", "%2A")
                .Replace("'", "%27")
                .Replace("(", "%28")
                .Replace(")", "%29");
        }

        /// <summary>convert urlencoded querystring</summary>
        public static string ToQueryParameter(this IEnumerable<KeyValuePair<string, object>> parameters)
        {
            return string.Join("&", parameters.Select(p => p.Key.UrlEncode() + '=' + p.Value.ToString().UrlEncode()));
        }

        public static IObservable<WebResponse> GetResponseAsObservable(this WebRequest request)
        {
            return Observable.FromAsyncPattern<WebResponse>(request.BeginGetResponse, request.EndGetResponse)();
        }

        public static IObservable<Stream> GetRequestStreamAsObservable(this WebRequest request)
        {
            return Observable.FromAsyncPattern<Stream>(request.BeginGetRequestStream, request.EndGetRequestStream)();
        }

        /// <summary>Asynchronously gets responses terminated by line feeds.</summary>
        public static IObservable<string> GetResponseLines(this IObservable<WebResponse> response)
        {
            return response.SelectMany(x => x.GetLines());
        }

        public static IObservable<byte[]> GetBytes(this WebResponse response)
        {
            var stream = response.GetResponseStream();
            var reader = Observable.FromAsyncPattern<byte[], int, int, int>(stream.BeginRead, stream.EndRead);
            return Observable.Return(new byte[512])
                .SelectMany(x => reader(x, 0, x.Length), (buffer, count) => new { buffer, count })
                .Repeat()
                .TakeWhile(x => x.count > 0)
                .Select(x =>
                {
                    var result = new byte[x.count];
                    Buffer.BlockCopy(x.buffer, 0, result, 0, x.count);
                    return result;
                })
                .Finally(stream.Close);
        }

        public static IObservable<string> GetLines(this WebResponse response)
        {
            return Observable.Create<string>(ob =>
            {
                var sb = new StringBuilder();
                return response
                    .GetBytes()
                    .Subscribe(
                        x =>
                        {
                            foreach (char c in x)
                            {
                                sb.Append(c);
                                if (c == '\n')
                                {
                                    ob.OnNext(sb.ToString());
                                    sb.Clear();
                                }
                            }
                        },
                        () =>
                        {
                            ob.OnNext(sb.ToString());
                            ob.OnCompleted();
                        });
            }).Where(x => !string.IsNullOrWhiteSpace(x));
        }
    }
}