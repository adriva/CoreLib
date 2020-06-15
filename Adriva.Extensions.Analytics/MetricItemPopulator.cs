using Adriva.AppInsights.Serialization.Contracts;
using Adriva.Extensions.Analytics.Entities;

namespace Adriva.Extensions.Analytics
{
    internal sealed class MetricItemPopulator : AnalyticsItemPopulator
    {
        public override string TargetKey => "Metric";

        public override bool TryPopulate(Envelope envelope, ref AnalyticsItem analyticsItem)
        {
            if (!(envelope.EventData is MetricData metricData)) return false;
            if (null == metricData.Metrics || 0 == metricData.Metrics.Count) return false;

            foreach (var metric in metricData.Metrics)
            {
                MetricItem metricItem = new MetricItem()
                {
                    Kind = metric.Kind,
                    Maximum = metric.Maximum ?? 0,
                    Minimum = metric.Minimum ?? 0,
                    SampleRate = envelope.SampleRate,
                    StandardDeviation = metric.StandardDeviation ?? 0,
                    Value = metric.Value,
                    Name = metric.Name,
                    Count = metric.Count ?? 0
                };

                if (!string.IsNullOrWhiteSpace(metricItem.Name) && -1 < metricItem.Name.IndexOf('\\'))
                {
                    metricItem.NormalizedName = metricItem.Name.Substring(1 + metricItem.Name.LastIndexOf('\\'));
                }
                else
                {
                    metricItem.NormalizedName = metricItem.Name;
                }
                analyticsItem.Metrics.Add(metricItem);
            }

            return true;
        }
    }
}