using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Adriva.Extensions.Reports
{
    public class FilterOutput : IEnumerable<FilterItem>
    {
        protected readonly IEnumerable<FilterItem> Items;

        public ReportDefinition ReportDefinition { get; private set; }

        public FilterOutput(ReportDefinition reportDefinition, IEnumerable<FilterItem> items)
        {
            this.ReportDefinition = reportDefinition;
            this.Items = items;
        }

        public IEnumerator<FilterItem> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Items).GetEnumerator();
        }
    }

    public class FilterOutput<TOptions> : IEnumerable<FilterItem<TOptions>>
    {
        protected readonly IEnumerable<FilterItem<TOptions>> Items;

        public ReportDefinition ReportDefinition { get; private set; }

        public FilterOutput(ReportDefinition reportDefinition, IEnumerable<FilterItem<TOptions>> items)
        {
            this.ReportDefinition = reportDefinition;
            this.Items = items;
        }

        public FilterOutput(FilterOutput output)
        {
            this.ReportDefinition = output.ReportDefinition;
            this.Items = output.OfType<FilterItem<TOptions>>().AsEnumerable();
        }

        public IEnumerator<FilterItem<TOptions>> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Items).GetEnumerator();
        }
    }
}