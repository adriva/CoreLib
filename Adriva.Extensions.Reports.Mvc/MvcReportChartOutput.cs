using System.Collections.Generic;

namespace Adriva.Extensions.Reports.Mvc
{

    public class MvcReportChartOutput : ReportOutput
    {
        public IEnumerable<object> XAxisTitles { get; set; }

        public IList<MvcChartSeries> Series { get; private set; } = new List<MvcChartSeries>();

        public MvcReportChartOutput(string name, ReportDefinition reportDefinition, IDataSet data, FilterValues filterValues)
            : base(name, reportDefinition, data, filterValues)
        {

        }
    }
}