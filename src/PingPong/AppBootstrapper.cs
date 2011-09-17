using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autofac;
using Caliburn.Micro;
using PingPong.Timelines;

namespace PingPong
{
    public partial class AppBootstrapper : Bootstrapper<IShell>
    {
        public const string Version = "0.0.0.1";
        public const string UserAgentVersion = "Ping.Pong v" + Version;

        private IContainer _container;

        protected override void Configure()
        {
            LogManager.GetLog = t => new DebugLog();

            var b = new ContainerBuilder();
            b.RegisterAssemblyTypes(GetType().Assembly)
                .AsSelf()
                .AsImplementedInterfaces()
                .PropertiesAutowired();
            b.RegisterAssemblyTypes(GetType().Assembly)
                .Where(t => t.IsAssignableTo<Timeline>())
                .AsSelf()
                .PropertiesAutowired()
                .OnActivated(x => ((dynamic)x.Instance).Start());
            b.Register(_ => new TwitterClient()).SingleInstance();
            b.Register(_ => new WindowManager()).As<IWindowManager>().SingleInstance();
            b.Register(_ => new EventAggregator { PublicationThreadMarshaller = Execute.OnUIThread }).As<IEventAggregator>().SingleInstance();

            _container = b.Build();
        }

        protected override object GetInstance(Type serviceType, string key)
        {
            return string.IsNullOrEmpty(key) ? _container.Resolve(serviceType) : _container.ResolveNamed(key, serviceType);
        }

        protected override IEnumerable<object> GetAllInstances(Type serviceType)
        {
            return ((IEnumerable)_container.Resolve(typeof(IEnumerable<>).MakeGenericType(serviceType))).Cast<object>();
        }

        private class DebugLog : ILog
        {
            public void Info(string format, params object[] args)
            {
                Debug.WriteLine("[INFO ] " + format, args);
            }

            public void Warn(string format, params object[] args)
            {
                Debug.WriteLine("[DEBUG] " + format, args);
            }

            public void Error(Exception exception)
            {
                Debug.WriteLine("[ERROR] " + exception);
            }
        }
    }
}