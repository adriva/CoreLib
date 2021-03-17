using System;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Options;

namespace Adriva.AppInsights
{
    internal sealed class BaseTelemetryInitializer : ITelemetryInitializer
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly Action<IServiceProvider, ITelemetry> InitializeFunction;

        public BaseTelemetryInitializer(IServiceProvider serviceProvider, IOptions<AnalyticsOptions> optionsAccessor)
        {
            this.ServiceProvider = serviceProvider;
            this.InitializeFunction = optionsAccessor?.Value?.Initializer ?? ((sp, t) => { });
        }

        public void Initialize(ITelemetry telemetry)
        {
            this.InitializeFunction.Invoke(this.ServiceProvider, telemetry);
        }
    }
}