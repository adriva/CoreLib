using System;
using Microsoft.Extensions.DependencyInjection;

namespace Adriva.Extensions.Social
{
    public static class SocialMediaClientExtensions
    {

        public static IServiceCollection AddSocialMediaClient<TClient, TOptions>(this IServiceCollection services, Action<TOptions> configure)
            where TClient : class, ISocialMediaClient
            where TOptions : class, ISocialMediaClientOptions
        {
            services.AddSingleton<TClient, TClient>();
            services.Configure<TOptions>(configure);
            return services;
        }

        public static IServiceCollection AddTransientSocialMediaClient<TClient, TOptions>(this IServiceCollection services, Action<TOptions> configure)
                    where TClient : class, ISocialMediaClient
                    where TOptions : class, ISocialMediaClientOptions
        {
            services.AddTransient<TClient, TClient>();
            services.Configure<TOptions>(configure);
            return services;
        }
    }
}