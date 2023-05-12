using System;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Adriva.AppInsights
{
    internal sealed class BaseTelemetryFilter : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor Next;
        private readonly Func<ITelemetry, bool> FilterFunction;
        private readonly AnalyticsOptions Options;

        public BaseTelemetryFilter(ITelemetryProcessor next, IOptions<AnalyticsOptions> optionsAccessor)
        {
            this.Next = next;
            this.Options = optionsAccessor.Value;
            this.FilterFunction = (telemetry) =>
            {
                if (telemetry is RequestTelemetry requestTelemetry)
                {
                    if (Convert.ToString(requestTelemetry.Url).StartsWith(this.Options.EndPointAddress, StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                if (null != this.Options.Filter)
                {
                    return this.Options.Filter.Invoke(telemetry);
                }

                return true;
            };
        }

        public void Process(ITelemetry item)
        {
            if (!this.FilterFunction.Invoke(item)) return;
            this.Next.Process(item);
        }
    }
}