using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autofac;
using Autofac.Core;
using Caliburn.Micro;
using PingPong.Timelines;
using Parameter = Autofac.Core.Parameter;

namespace PingPong
{
    public partial class AppBootstrapper : Bootstrapper<IShell>
    {
        public const string Version = "0.0.0.2";
        public const string UserAgentVersion = "Ping.Pong v" + Version;

        private IContainer _container;

        protected override void Configure()
        {
            LogManager.GetLog = t => new DebugLog();

            var b = new ContainerBuilder();
            b.RegisterAssemblyTypes(GetType().Assembly)
                .AsSelf()
                .AsImplementedInterfaces()
                .PropertiesAutowired()
                .OnActivated(x => x.Context.Resolve<IEventAggregator>().Subscribe(x.Instance));
            b.RegisterAssemblyTypes(GetType().Assembly)
                .Where(t => t.IsAssignableTo<Timeline>())
                .AsSelf()
                .PropertiesAutowired()
                .OnActivated(x => ((dynamic)x.Instance).Start())
                .OnActivated(x => x.Context.Resolve<IEventAggregator>().Subscribe(x.Instance));
            b.Register(_ => new TwitterClient()).SingleInstance();
            b.Register(_ => new WindowManager()).As<IWindowManager>().SingleInstance();
            b.Register(_ => new EventAggregator { PublicationThreadMarshaller = Execute.OnUIThread }).As<IEventAggregator>().SingleInstance();
            b.Register(_ => new ShellViewModel(_container)).As<IShell>().SingleInstance();
            b.Register(_ => _.Resolve<IShell>()).Named<IShell>("shell").SingleInstance();

            _container = b.Build();

            Application.Exit += delegate { _container.Dispose(); };
        }

        protected override object GetInstance(Type serviceType, string key)
        {
            if (string.IsNullOrEmpty(key))
                return _container.Resolve(serviceType);

            var registration = (from r in _container.ComponentRegistry.Registrations
                                from s in r.Services
                                let ks = s as KeyedService
                                where ks != null
                                where ks.ServiceKey.Equals(key)
                                select r).First();
            return _container.ResolveComponent(registration, Enumerable.Empty<Parameter>());
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