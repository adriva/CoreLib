using System;
using System.Collections.Generic;
using Adriva.Extensions.Analytics.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Adriva.Extensions.Analytics
{
    public static class Extensions
    {

        public static IServiceCollection AddAnalyticsServer(this IServiceCollection services, Action<AnalyticsServerOptions> configure)
        {
            services
                .Configure(configure)

                .AddDbContext<AnalyticsDbContext>((serviceProvider, builder) =>
                {
                    var optionsAccessor = serviceProvider.GetService<IOptions<AnalyticsServerOptions>>();
                    builder.UseSqlServer(optionsAccessor.Value.ConnectionString);
                }, ServiceLifetime.Scoped, ServiceLifetime.Singleton)

                .AddSingleton<IQueueingService, QueueingService>()
                .AddHostedService<ProcessorService>()
                ;

            services.TryAddEnumerable(ServiceDescriptor.Singleton<AnalyticsItemPopulator, RequestItemPopulator>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<AnalyticsItemPopulator, ExceptionItemPopulator>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<AnalyticsItemPopulator, MetricItemPopulator>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<AnalyticsItemPopulator, EventItemPopulator>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<AnalyticsItemPopulator, DependencyItemPopulator>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<AnalyticsItemPopulator, MessageItemPopulator>());

            return services;
        }

        public static IApplicationBuilder UseAnalyticsServer(this IApplicationBuilder app)
        {
            var optionsAccessor = app.ApplicationServices.GetService<IOptions<AnalyticsServerOptions>>();

            app.Map(optionsAccessor.Value.Endpoint, builder =>
            {
                builder.UseMiddleware<AnalyticsServerMiddleware>();
            });
            return app;
        }

        internal static bool TryGetString(this IDictionary<string, string> dictionary, string name, out string value)
        {
            return dictionary.TryGetValue(name, out value);
        }

        internal static bool TryGetBoolean(this IDictionary<string, string> dictionary, string name, out bool value)
        {
            value = false;

            if (!dictionary.TryGetString(name, out string stringValue)) return false;

            return bool.TryParse(stringValue, out value);
        }

    }
}
