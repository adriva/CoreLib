using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Adriva.Extensions.Reports
{
    public sealed class ReportDefinition
    {
        public string Title { get; set; }

        public string BaseReport { get; set; }

        public string ContextProvider { get; set; }

        public IDictionary<string, DynamicTypeDefinition> DataSources { get; set; }

        public IDictionary<string, QueryDefinition> Queries { get; set; }

        public IList<FilterDefinition> Filters { get; set; } = new List<FilterDefinition>();

        public ReportOutputDefinition Output { get; set; } = new ReportOutputDefinition();

        internal IDictionary<string, IConfigurationSection> DataSourceConfigurations { get; set; }

    }
}