using System.Collections.Generic;

namespace Adriva.Extensions.Reports
{
    public interface IQueryBuilder
    {
        IQuery Build(IParameterBinder parameterBinder, ReportingContext context);
    }

    public interface IDebugDataProvider
    {
        IDictionary<string, object> GetDebugData();
    }
}