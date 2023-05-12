using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Adriva.Extensions.Reports
{
    public static class ReportDefinitionExtensions
    {
        public static bool TryGetFilter(this ReportDefinition reportDefinition, string name, out FilterDefinition filterDefinition)
        {
            filterDefinition = null;
            if (string.IsNullOrWhiteSpace(name)) return false;

            var matchingFilter = reportDefinition.GetFilters().FirstOrDefault(f => 0 == string.Compare(f.Name, name, StringComparison.OrdinalIgnoreCase));
            if (null == matchingFilter)
            {
                name = "@" + name;
                matchingFilter = reportDefinition.GetFilters().FirstOrDefault(f => 0 == string.Compare(f.Name, name, StringComparison.OrdinalIgnoreCase));
            }

            filterDefinition = matchingFilter;
            return null != filterDefinition;
        }

        public static IEnumerable<FilterDefinition> GetFilters(this ReportDefinition reportDefinition)
        {
            if (null == reportDefinition.Filters) yield break;

            Queue<FilterDefinition> queue = new Queue<FilterDefinition>(reportDefinition.Filters);

            while (0 < queue.Count)
            {
                var filterDefinition = queue.Dequeue();

                if (null != filterDefinition.Filters)
                {
                    foreach (var childFilterDefinition in filterDefinition.Filters)
                    {
                        queue.Enqueue(childFilterDefinition);
                    }
                }

                yield return filterDefinition;
            }
        }

        public static QueryDefinition GetQuery(this ReportDefinition reportDefinition, string queryName)
        {
            if (reportDefinition.Queries.TryGetValue(queryName, out QueryDefinition queryDefinition))
            {
                if (!queryDefinition.SlidingExpiration.HasValue)
                    queryDefinition.SlidingExpiration = TimeSpan.FromSeconds(30);
                return queryDefinition;
            }
            return null;
        }

        public static void GetDataSource(this ReportDefinition reportDefinition, string dataSourceName, out string dataSourceTypeName, out IConfigurationSection configuration)
        {
            dataSourceTypeName = null;
            configuration = null;

            if (string.IsNullOrWhiteSpace(dataSourceName)) return;

            reportDefinition.DataSources.TryGetValue(dataSourceName, out DynamicTypeDefinition typeDefinition);
            reportDefinition.DataSourceConfigurations.TryGetValue(dataSourceName, out configuration);

            dataSourceTypeName = typeDefinition?.Type ?? null;
        }
    }
}