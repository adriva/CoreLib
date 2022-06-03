using System;
using Microsoft.ApplicationInsights.Channel;

namespace Adriva.AppInsights
{
    public class AnalyticsOptions
    {
        public string InstrumentationKey { get; set; }

        public string EndPointAddress { get; set; }

        public int BacklogSize { get; set; } = 100000;

        public int Capacity { get; set; } = 500;

        public bool IsDeveloperMode { get; set; }

        public bool ShouldCaptureHttpContent { get; set; }

        public Func<ITelemetry, bool> Filter { get; set; } = _ => true;

        public Action<IServiceProvider, ITelemetry> Initializer { get; set; } = (sp, t) => { };
    }
}