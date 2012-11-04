using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;

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

        public static IObservable<string> GetLines(this WebResponse response)
        {
            return Observable.Create<string>(ob =>
            {
                try
                {
                    using (var stream = response.GetResponseStream())
                    using (var reader = new StreamReader(stream))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            Debug.WriteLine(line);
                            if (!string.IsNullOrWhiteSpace(line))
                                ob.OnNext(line);
                        }
                    }
                }
                catch (Exception e)
                {
                    ob.OnError(e);
                }
                finally
                {
                    ob.OnCompleted();
                }

                return response;
            });
        }
    }
}