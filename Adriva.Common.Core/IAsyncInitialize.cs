using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Adriva.Common.Core {

	public interface IAsyncInitialize {

		Task InitializeAsync();

	}

	public interface IAsyncInitialize<T>
	{
		Task InitializeAsync(T data);
	}

    public static class ServiceCollectionExtensions
    {

        public static IServiceCollection AddAsyncSingleton<TService, TImplementation>(this IServiceCollection services)
            where TService: class
            where TImplementation : class, TService, IAsyncInitialize
        {

            services.AddSingleton<TService, TImplementation>((serviceProvider) => {
                TImplementation instance = ActivatorUtilities.CreateInstance<TImplementation>(serviceProvider);
                Task initializationTask = instance.InitializeAsync();
                initializationTask.Wait();
                return instance;
            });

            return services;
        }

        public static IServiceCollection AddAsyncSingleton<TService, TImplementation, TData>(this IServiceCollection services, TData data)
            where TService : class
            where TImplementation : class, TService, IAsyncInitialize<TData>
        {

            services.AddSingleton<TService, TImplementation>((serviceProvider) => {
                TImplementation instance = ActivatorUtilities.CreateInstance<TImplementation>(serviceProvider);
                Task initializationTask = instance.InitializeAsync(data);
                initializationTask.Wait();
                return instance;
            });

            return services;
        }

        public static IServiceCollection AddAsyncSingleton<TService, TImplementation, TData>(this IServiceCollection services, Func<TData> dataFactory)
            where TService : class
            where TImplementation : class, TService, IAsyncInitialize<TData>
        {

            services.AddSingleton<TService, TImplementation>((serviceProvider) => {
                TImplementation instance = ActivatorUtilities.CreateInstance<TImplementation>(serviceProvider);
                TData parameter = dataFactory.Invoke();
                Task initializationTask = instance.InitializeAsync(parameter);
                initializationTask.Wait();
                return instance;
            });

            return services;
        }
    }
}
