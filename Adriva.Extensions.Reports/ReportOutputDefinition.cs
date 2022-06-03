using System.Collections.Generic;

namespace Adriva.Extensions.Reports
{
    public sealed class ReportOutputDefinition : RendererOptionsProvider
    {
        public string DataSource { get; set; }

        public string Query { get; set; }

        public PagingDefinition Paging { get; set; }

        public IList<ReportColumnDefinition> ColumnDefinitions { get; set; } = new List<ReportColumnDefinition>();
    }
}