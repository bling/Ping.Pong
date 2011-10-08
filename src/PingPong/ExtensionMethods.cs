using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Caliburn.Micro;
using Action = System.Action;

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
        public static IDisposable DispatcherSubscribe<T>(this IObservable<T> observable, Action<T> onNext, Action<Exception> onError = null, Action onCompleted = null)
        {
            return observable
                .SubscribeOnThreadPool()
                .ObserveOnDispatcher()
                .Subscribe(onNext, onError ?? (e => { throw e; }), onCompleted ?? (() => { }));
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

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> enumerable) where T : class
        {
            return enumerable.Where(x => x != null);
        }

        public static IObservable<T> WhereNotNull<T>(this IObservable<T> observable) where T : class
        {
            return observable.Where(x => x != null);
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
            if ((value == null && field != null) ||
                !value.Equals(field))
            {
                field = value;
                viewModel.NotifyOfPropertyChange(propertyName);
                return true;
            }
            return false;
        }

        public static void CopyProperties<T>(this T target, T source)
        {
            var props = from p in typeof(T).GetProperties()
                        where p.GetIndexParameters().Length == 0
                        where p.CanWrite
                        where p.GetSetMethod(false) != null
                        select p;
            foreach (var prop in props)
                prop.SetValue(target, prop.GetValue(source, null), null);
        }
    }
}