using System.Collections.Generic;

namespace Adriva.Extensions.Reports
{
    public interface IDataSet
    {
        int PageIndex { get; }

        int PageCount { get; }

        bool HasMore { get; }

        object NextPageToken { get; }

        string[] FieldNames { get; }

        IEnumerable<IDataItem> Items { get; }

        IDictionary<string, object> Metadata { get; }
    }
}