using Adriva.AppInsights.Serialization.Contracts;

namespace Adriva.Extensions.Analytics.Entities
{
    public class MessageItem
    {
        public long Id { get; set; }

        public long AnalyticsItemId { get; set; }

        public string Category { get; set; }

        public string Message { get; set; }

        public string Environment { get; set; }

        public bool? IsDeveloperMode { get; set; }

        public SeverityLevel? Severity { get; internal set; }

        public AnalyticsItem AnalyticsItem { get; set; }
    }
}