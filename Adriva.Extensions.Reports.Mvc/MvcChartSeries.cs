using System.Collections.Generic;

namespace Adriva.Extensions.Reports.Mvc
{
    public sealed class MvcChartSeries
    {
        public string Title { get; private set; }

        public IEnumerable<decimal> Values { get; private set; }

        public MvcChartSeries(string title, IEnumerable<decimal> values)
        {
            this.Title = title;
            this.Values = values;
        }
    }
}