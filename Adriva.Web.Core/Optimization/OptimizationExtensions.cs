using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using WebMarkupMin.AspNetCore2;

namespace Adriva.Web.Core.Optimization
{
    public static class OptimizationExtensions
    {
        public static IApplicationBuilder UseOptimization(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<OptimizationMiddleware>();
            builder.UseWebMarkupMin();
            return builder;
        }

        public static IServiceCollection AddOptimization(this IServiceCollection services, Action<OptimizationOptions> configureOptions)
        {
            OptimizationOptions dummyOptions = new OptimizationOptions();
            configureOptions?.Invoke(dummyOptions);

            services.AddSingleton<IAssetPipelineManager, AssetPipelineManager>();
            services.AddSingleton(typeof(AssetStore), dummyOptions.StoreType);

            services.AddScoped<IOptimizationContext, OptimizationContext>();
            services.AddWebMarkupMin((options) =>
            {
                options.DisablePoweredByHttpHeaders = true;
                options.AllowMinificationInDevelopmentEnvironment = dummyOptions.OptimizeHtml;
                options.DisableMinification = !dummyOptions.OptimizeHtml;

            }).AddHtmlMinification(x => x.MinificationSettings.RemoveOptionalEndTags = false);

            services.Configure<OptimizationOptions>((options) =>
            {
                options.Orderer = new DefaultAssetOrderer();
                configureOptions?.Invoke(options);
            });

            return services;
        }
        internal static IEnumerable<AssetFile> ApplyOrderer(this IEnumerable<AssetFile> files, IAssetOrderer orderer)
        {
            if (null == files) return Array.Empty<AssetFile>();
            if (null == orderer) return files;

            return orderer.Order(files);
        }

    }
}
