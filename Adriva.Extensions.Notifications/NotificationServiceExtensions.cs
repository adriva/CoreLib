using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Adriva.Extensions.Notifications
{
    public static class NotificationServiceExtensions
    {
        public static INotificationServiceBuilder AddNotificationService(this IServiceCollection services)
        {
            return services.AddNotificationService<NotificationService>();
        }

        public static INotificationServiceBuilder AddNotificationService<TService>(this IServiceCollection services) where TService : class, INotificationService
        {
            NotificationServiceBuilder builder = new NotificationServiceBuilder(services);
            services.TryAddSingleton<INotificationService, TService>();
            services.TryAddSingleton<INotificationServiceBuilder>(builder);

            return builder;
        }
    }
}
