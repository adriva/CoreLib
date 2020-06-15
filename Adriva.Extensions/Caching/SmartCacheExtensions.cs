using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Adriva.Extensions.Caching
{
    public static class SmartCacheExtensions
    {

        public static IServiceCollection AddSmartCache(this IServiceCollection services, Action<MemoryCacheOptions> configure)
        {
            services.AddSingleton<ISmartCache, SmartCache<SmartCacheChangeToken>>();
            services.Configure<MemoryCacheOptions>(configure);
            return services;
        }

        public static IServiceCollection AddSmartCache<T>(this IServiceCollection services, Action<MemoryCacheOptions> configure) where T : SmartCacheChangeToken, new()
        {
            services.AddSingleton<ISmartCache, SmartCache<T>>();
            services.Configure<MemoryCacheOptions>(configure);
            return services;
        }

    }
}
