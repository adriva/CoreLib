using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Adriva.Extensions.Notifications
{
    internal class NotificationServiceBuilder : INotificationServiceBuilder
    {
        private int InputSinkCount = 0;
        public IServiceCollection Services { get; private set; }

        public NotificationServiceBuilder(IServiceCollection services)
        {
            this.Services = services;
        }

        public INotificationServiceBuilder AddSink<TSink>() where TSink : NotificationSink
        {
            this.Services.AddSingleton<NotificationSinkWrapper>(serviceProvider =>
            {
                TSink instance = ActivatorUtilities.CreateInstance<TSink>(serviceProvider);
                var wrapper = new NotificationSinkWrapper(instance, Interlocked.Increment(ref this.InputSinkCount));
                return wrapper;
            });
            return this;
        }

        public INotificationServiceBuilder AddPublisher<TPublisher, TPublisherOptions>(Action<TPublisherOptions> configure)
                where TPublisher : class, INotificationPublisher
                where TPublisherOptions : class
        {
            this.Services.AddSingleton<INotificationPublisher, TPublisher>();
            this.Services.Configure(configure);
            return this;
        }

        public INotificationServiceBuilder AddPublisher<TPublisher>() where TPublisher : class, INotificationPublisher
        {
            this.Services.AddSingleton<INotificationPublisher, TPublisher>();
            return this;
        }

        public INotificationServiceBuilder UseStore<TStore, TStoreOptions>(Action<TStoreOptions> configure)
                where TStore : class, INotificationStore
                where TStoreOptions : class
        {
            this.Services.TryAddSingleton<INotificationStore, TStore>();
            this.Services.Configure(configure);
            return this;
        }

        public INotificationServiceBuilder UseStore<TStore>() where TStore : class, INotificationStore
        {
            this.Services.TryAddSingleton<INotificationStore, TStore>();
            return this;
        }
    }
}