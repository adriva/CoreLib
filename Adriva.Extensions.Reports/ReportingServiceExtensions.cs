using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Adriva.Extensions.Reports
{
    public static class ReportingServiceExtensions
    {
        private class ReportingServiceBuilder : IReportingServiceBuilder
        {
            public IServiceCollection Services { get; private set; }

            public ReportingServiceBuilder(IServiceCollection services)
            {
                this.Services = services;
            }
        }

        public static IReportingServiceBuilder AddReporting(this IServiceCollection services, Action<ReportingServiceOptions> configureOptions)
        {
            services.TryAddSingleton<IMemoryCache, MemoryCache>();
            services.TryAddSingleton<IReportRendererFactory, ReportRendererFactory>();
            services.TryAddSingleton<IParameterBinder, ParameterBinder>();
            services.AddSingleton<IReportingService>(serviceProvider =>
            {
                IReportingService reportingService = ActivatorUtilities.CreateInstance<ReportingService>(serviceProvider);
                return reportingService;
            });
            services.Configure(configureOptions);
            return new ReportingServiceBuilder(services);
        }

    }
}