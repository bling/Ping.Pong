using System;
using System.IO;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;

namespace PingPong.OAuth
{
    internal static class WebRequestExtensions
    {
        public static IObservable<WebResponse> GetResponseAsObservable(this WebRequest request)
        {
            return Observable.FromAsyncPattern<WebResponse>(request.BeginGetResponse, request.EndGetResponse)();
        }

        public static IObservable<Stream> GetRequestStreamAsObservable(this WebRequest request)
        {
            return Observable.FromAsyncPattern<Stream>(request.BeginGetRequestStream, request.EndGetRequestStream)();
        }
    }

    internal static class WebResponseExtensions
    {
        public static IObservable<byte[]> GetBytes(this WebResponse response)
        {
            return Observable.Create<byte[]>(ob =>
            {
                var stream = response.GetResponseStream();
                var reader = Observable.FromAsyncPattern<byte[], int, int, int>(stream.BeginRead, stream.EndRead);
                var buffer = new byte[256];
                while (true)
                {
                    int bytesRead = reader(buffer, 0, buffer.Length).Single();
                    if (bytesRead == 0)
                    {
                        ob.OnCompleted();
                        break;
                    }

                    var result = new byte[bytesRead];
                    Buffer.BlockCopy(buffer, 0, result, 0, bytesRead);
                    ob.OnNext(result);
                }
                return stream;
            });
        }

        public static IObservable<string> GetLines(this WebResponse response)
        {
            return Observable.Create<string>(ob =>
            {
                var sb = new StringBuilder();
                response
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

                return Disposable.Empty;
            });
        }
    }
}