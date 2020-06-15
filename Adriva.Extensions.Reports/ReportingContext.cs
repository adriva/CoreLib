namespace Adriva.Extensions.Reports
{
    public class ReportingContext
    {
        public QueryDefinition Query { get; private set; }

        public ReportDefinition Report { get; private set; }

        public FilterValues Values { get; private set; }

        public object ContextProvider { get; private set; }

        public ReportingContext(QueryDefinition query, ReportDefinition report, FilterValues values, object contextProvider)
        {
            this.Query = query;
            this.Report = report;
            this.Values = values;
            this.ContextProvider = contextProvider;
        }
    }
}