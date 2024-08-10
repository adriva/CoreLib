using System;
using Microsoft.Extensions.DependencyInjection;

namespace Adriva.Extensions.Notifications
{
    public interface INotificationServiceBuilder
    {
        IServiceCollection Services { get; }

        INotificationServiceBuilder AddSink<TSink>() where TSink : NotificationSink;

        INotificationServiceBuilder AddPublisher<TPublisher, TPublisherOptions>(Action<TPublisherOptions> configure)
                                                                                                        where TPublisher : class, INotificationPublisher
                                                                                                        where TPublisherOptions : class;

        INotificationServiceBuilder AddPublisher<TPublisher>() where TPublisher : class, INotificationPublisher;

        INotificationServiceBuilder UseStore<TStore, TStoreOptions>(Action<TStoreOptions> configure)
                                                                                    where TStore : class, INotificationStore
                                                                                    where TStoreOptions : class;

        INotificationServiceBuilder UseStore<TStore>() where TStore : class, INotificationStore;
    }
}
