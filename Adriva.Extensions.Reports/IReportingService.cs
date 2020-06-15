using System;
using System.Threading.Tasks;

namespace Adriva.Extensions.Reports
{
    public interface IReportingService
    {
        Task<ReportDefinition> LoadReportAsync(string name);

        Task<ReportOutput> GetReportOutputAsync(string name, FilterValues filterValues, Func<ReportDefinition, IQuery, bool> checkSkipPopulateData = null);

        Task<FilterOutput> GetFilterOutputAsync(string name, FilterValues filterValues);

        Task<ValueType> ExecuteQueryAsync(string name, string queryName, FilterValues filterValues);
    }
}