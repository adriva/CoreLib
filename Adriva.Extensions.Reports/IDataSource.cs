using System;
using System.Threading.Tasks;

namespace Adriva.Extensions.Reports
{
    internal interface IDataSource
    {
        IQueryBuilder CreateQueryBuilder();

        Task OpenAsync();

        Task CloseAsync();

        Task<IDataSet> GetDataAsync(IQuery query);

        Task<ValueType> ExecuteAsync(IQuery query);
    }
}