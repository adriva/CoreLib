namespace Adriva.Extensions.Analytics.Entities
{
    public class EventItem
    {
        public long Id { get; set; }

        public long AnalyticsItemId { get; set; }

        public string Name { get; set; }

        public string Environment { get; set; }

        public bool? IsDeveloperMode { get; set; }

        public AnalyticsItem AnalyticsItem { get; set; }
    }
}