using System;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Adriva.Extensions.Http
{
    public static class HttpClientWrapperExtensions
    {
        public static IHttpClientBuilder AddHttpClientWrapper(this IServiceCollection services, Action<HttpClientHandler> configureHandler = null)
        {
            return services.AddHttpClientWrapper<HttpClientWrapper>(true, configureHandler);
        }

        public static IHttpClientBuilder AddHttpClientWrapper(this IServiceCollection services, bool useCookies, Action<HttpClientHandler> configureHandler = null)
        {
            return services.AddHttpClientWrapper<HttpClientWrapper>(useCookies, configureHandler);
        }

        public static IHttpClientBuilder AddHttpClientWrapper<T>(this IServiceCollection services, bool useCookies, Action<HttpClientHandler> configureHandler = null) where T : HttpClientWrapper
        {
            return services
                .AddSingleton<CookieContainer>()
                .AddHttpClient<T>()
                .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
                {
                    var handler = new HttpClientHandler();
                    handler.AllowAutoRedirect = true;
                    handler.AutomaticDecompression = handler.SupportsAutomaticDecompression ? DecompressionMethods.Deflate | DecompressionMethods.GZip : DecompressionMethods.None;
                    handler.UseCookies = useCookies;

                    if (useCookies)
                    {
                        handler.CookieContainer = serviceProvider.GetRequiredService<CookieContainer>();
                    }

                    configureHandler?.Invoke(handler);

                    return handler;
                });
        }
    }
}