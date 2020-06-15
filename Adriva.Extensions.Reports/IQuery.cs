using System.Collections.Generic;

namespace Adriva.Extensions.Reports
{
    public interface IQuery
    {
        string CommandText { get; }

        IList<QueryParameter> Parameters { get; }
    }
}