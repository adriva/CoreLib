using Adriva.AppInsights.Serialization.Contracts;

namespace Adriva.Extensions.Analytics.Entities
{
    public class RequestItem
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Environment { get; set; }

        public bool? IsDeveloperMode { get; set; }

        public double Duration { get; set; } //in msecs.

        public bool IsSuccess { get; set; }

        public int ResponseCode { get; set; }

        public string Url { get; set; }

        public AnalyticsItem AnalyticsItem { get; set; }
    }
}