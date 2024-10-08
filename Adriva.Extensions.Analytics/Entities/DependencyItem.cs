namespace Adriva.Extensions.Analytics.Entities
{
    public class DependencyItem
    {
        public long Id { get; set; }

        public long AnalyticsItemId { get; set; }

        public string Name { get; set; }

        public string Target { get; set; }

        public double Duration { get; set; }

        public bool? IsSuccess { get; set; }

        public string Type { get; set; }

        public string Environment { get; set; }

        public bool? IsDeveloperMode { get; set; }

        public string Input { get; set; }

        public string Output { get; set; }

        public string InputHeaders { get; set; }

        public string OutputHeaders { get; set; }

        public AnalyticsItem AnalyticsItem { get; set; }
    }
}