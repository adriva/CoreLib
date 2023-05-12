using System;
using System.Collections.Generic;
using System.Linq;

namespace Adriva.Extensions.Reports
{
    public class DataSet : IDataSet
    {
        public static DataSet Empty => new DataSet()
        {
            PageIndex = 0,
            PageCount = 0,
            HasMore = false,
            NextPageToken = null,
            FieldNames = Array.Empty<string>(),
            Items = Enumerable.Empty<IDataItem>()
        };

        public int PageIndex { get; protected internal set; }

        public int PageCount { get; protected internal set; }

        public bool HasMore { get; protected internal set; }

        public object NextPageToken { get; protected set; }

        public string[] FieldNames { get; protected set; }

        public virtual IEnumerable<IDataItem> Items { get; private set; }

        public IDictionary<string, object> Metadata { get; private set; } = new Dictionary<string, object>();
    }
}