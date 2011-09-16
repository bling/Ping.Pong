using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Caliburn.Micro;

namespace PingPong
{
    [DebuggerStepThrough]
    public static class ExtensionMethods
    {
        public static IObservable<T> SubscribeOnThreadPool<T>(this IObservable<T> observable)
        {
            return observable.SubscribeOn(Scheduler.ThreadPool);
        }

        public static IObservable<T> ObserveOnThreadPool<T>(this IObservable<T> observable)
        {
            return observable.ObserveOn(Scheduler.ThreadPool);
        }

        /// <summary>
        /// Applies the default SubscribeOnThreadPool/ObserveOnDispatcher/Subscribe(action) pattern.
        /// </summary>
        public static IDisposable DispatcherSubscribe<T>(this IObservable<T> observable, Action<T> onNext)
        {
            return observable
                .SubscribeOnThreadPool()
                .ObserveOnDispatcher()
                .Subscribe(onNext);
        }

        public static void DisposeIfNotNull(this IDisposable disposable)
        {
            if (disposable != null)
                disposable.Dispose();
        }

        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (var element in collection)
                action(element);
        }

        public static string UnescapeXml(this string xml)
        {
            return xml.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", "'");
        }

        public static IDictionary<string, string> ToQueryParameters(this string input)
        {
            return (from item in input.Split('&')
                    select item.Split('=')).ToDictionary(x => x[0], x => x[1]);
        }

        public static bool SetValue<T>(this INotifyPropertyChangedEx viewModel, string propertyName, T value, ref T field)
        {
            if (!value.Equals(field))
            {
                field = value;
                viewModel.NotifyOfPropertyChange(propertyName);
                return true;
            }
            return false;
        }
    }
}