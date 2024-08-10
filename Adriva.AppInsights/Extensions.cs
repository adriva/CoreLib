extern alias WebAppInsights;
extern alias WorkerAppInsights;

using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;

namespace Adriva.AppInsights
{
    public static class Extensions
    {

        public static IServiceCollection AddAnalytics(this IServiceCollection services, Action<AnalyticsOptions> configure, Action<WebAppInsights::Microsoft.ApplicationInsights.AspNetCore.Extensions.ApplicationInsightsServiceOptions> postConfigure = null)
        {
            AnalyticsOptions analyticsOptions = new AnalyticsOptions();
            configure?.Invoke(analyticsOptions);
            services.Configure<AnalyticsOptions>(baseOptions =>
            {
                baseOptions.BacklogSize = analyticsOptions.BacklogSize;
                baseOptions.Capacity = analyticsOptions.Capacity;
                baseOptions.EndPointAddress = analyticsOptions.EndPointAddress;
                baseOptions.InstrumentationKey = analyticsOptions.InstrumentationKey;
                baseOptions.IsDeveloperMode = analyticsOptions.IsDeveloperMode;
                baseOptions.ShouldCaptureHttpContent = analyticsOptions.ShouldCaptureHttpContent;
                baseOptions.Filter = analyticsOptions.Filter;
                baseOptions.Initializer = analyticsOptions.Initializer;
            });

            services.AddSingleton<ITelemetryChannel, ServerTelemetryChannel>();

            if (analyticsOptions.ShouldCaptureHttpContent)
            {
                services.AddSingleton<ITelemetryInitializer, RemoteDependencyDataInitializer>();
            }

            services.AddSingleton<ITelemetryInitializer, BaseTelemetryInitializer>();

            WebAppInsights::Microsoft.Extensions.DependencyInjection.ApplicationInsightsExtensions.AddApplicationInsightsTelemetry(services, options =>
            {
                options.InstrumentationKey = analyticsOptions.InstrumentationKey;
                options.EndpointAddress = analyticsOptions.EndPointAddress;

                if (null != postConfigure)
                {
                    postConfigure(options);
                }
            });

            WebAppInsights::Microsoft.Extensions.DependencyInjection.ApplicationInsightsExtensions.AddApplicationInsightsTelemetryProcessor<BaseTelemetryFilter>(services);

            WebAppInsights::Microsoft.Extensions.DependencyInjection.ApplicationInsightsExtensions.ConfigureTelemetryModule<QuickPulseTelemetryModule>(services, (module, options) =>
            {
                module.AuthenticationApiKey = analyticsOptions.InstrumentationKey;
                module.QuickPulseServiceEndpoint = analyticsOptions.EndPointAddress;
            });

            return services;
        }

        public static IServiceCollection AddWorkerAnalytics(this IServiceCollection services, Action<AnalyticsOptions> configure)
        {
            AnalyticsOptions analyticsOptions = new AnalyticsOptions();
            configure?.Invoke(analyticsOptions);
            services.Configure<AnalyticsOptions>(baseOptions =>
            {
                baseOptions.BacklogSize = analyticsOptions.BacklogSize;
                baseOptions.Capacity = analyticsOptions.Capacity;
                baseOptions.EndPointAddress = analyticsOptions.EndPointAddress;
                baseOptions.InstrumentationKey = analyticsOptions.InstrumentationKey;
                baseOptions.IsDeveloperMode = analyticsOptions.IsDeveloperMode;
                baseOptions.Filter = analyticsOptions.Filter;
                baseOptions.Initializer = analyticsOptions.Initializer;
            });

            services.AddSingleton<ITelemetryChannel, ServerTelemetryChannel>();
            services.AddSingleton<ITelemetryInitializer, BaseTelemetryInitializer>();

            WorkerAppInsights::Microsoft.Extensions.DependencyInjection.ApplicationInsightsExtensions.AddApplicationInsightsTelemetryWorkerService(services, options =>
            {
                options.InstrumentationKey = analyticsOptions.InstrumentationKey;
                options.DeveloperMode = analyticsOptions.IsDeveloperMode;
                options.EndpointAddress = analyticsOptions.EndPointAddress;
            });

            return services;
        }

        public static ILoggingBuilder AddAnalytics(this ILoggingBuilder builder, params KeyValuePair<string, LogLevel>[] logLevelMappings)
        {
            if (null != logLevelMappings)
            {
                foreach (var logLevelMapping in logLevelMappings)
                {
                    builder.AddFilter<ApplicationInsightsLoggerProvider>(logLevelMapping.Key, logLevelMapping.Value);
                }
            }
            return builder.AddApplicationInsights();
        }

    }
}