using System.Collections.Generic;
using System.Collections.ObjectModel;
using Adriva.Extensions.Analytics.Entities;

namespace Adriva.Extensions.Analytics
{
    public sealed class AnalyticsProcessorContext
    {
        private readonly List<AnalyticsItem> CachedItems = new List<AnalyticsItem>();

        public ReadOnlyCollection<AnalyticsItem> Items
        {
            get
            {
                return new ReadOnlyCollection<AnalyticsItem>(this.CachedItems);
            }
        }

        public void AddItems(IEnumerable<AnalyticsItem> analyticsItems)
        {
            if (null == analyticsItems) return;

            this.CachedItems.AddRange(analyticsItems);
        }

        public void AddItem(AnalyticsItem analyticsItem)
        {
            if (null == analyticsItem) return;

            this.CachedItems.Add(analyticsItem);
        }
    }
}