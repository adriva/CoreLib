using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Adriva.Common.Core
{

    public sealed class PagedResult<T>
    {
        [JsonIgnore]
        public static readonly PagedResult<T> Empty = new PagedResult<T>(Array.Empty<T>(), 0, 0, 0);

        [JsonProperty("pageIndex")]
        public int PageIndex { get; private set; }

        [JsonProperty("pageCount")]
        public int PageCount { get; private set; }

        [JsonProperty("recordCount")]
        public long RecordCount { get; private set; }

        [JsonProperty("items")]
        public IEnumerable<T> Items { get; private set; }

        public PagedResult(IEnumerable<T> items)
            : this(items, 0, 1, items.Count())
        {

        }

        public PagedResult(IEnumerable<T> items, int pageIndex, int pageCount, long recordCount)
        {
            this.Items = items;
            this.PageIndex = Math.Max(0, pageIndex);
            this.PageCount = Math.Max(0, pageCount);
            this.RecordCount = recordCount;
        }

        [JsonConstructor]
        public PagedResult()
        {

        }
    }
}
