using System;

namespace Adriva.Extensions.Reports
{
    public interface IParameterBinder
    {
        void Bind(IQuery query, ReportingContext context, Func<object, object> valueFormatter = null);
    }
}