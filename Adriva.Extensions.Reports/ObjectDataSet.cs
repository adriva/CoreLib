using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;

namespace Adriva.Extensions.Reports
{
    public class ObjectDataSet<TItem> : DataSet
    {
        private class ObjectDataItem : IDataItem
        {
            private readonly TItem InnerItem;
            private readonly IMemoryCache Cache;

            public ObjectDataItem(TItem innerItem, IMemoryCache cache)
            {
                this.InnerItem = innerItem;
                this.Cache = cache;
            }

            public object GetValue(string fieldName)
            {
                return ReflectionHelpers.GetPropertyValue(this.InnerItem, fieldName, this.Cache);
            }
        }

        private readonly IMemoryCache Cache;
        private readonly LinkedList<IDataItem> ItemsList = new LinkedList<IDataItem>();

        public override IEnumerable<IDataItem> Items => this.ItemsList;

        public ObjectDataSet(IMemoryCache cache)
        {
            this.Cache = cache;
            this.FieldNames = ReflectionHelpers.GetPropertyNames(typeof(TItem), this.Cache);
        }


        public void AddItem(TItem item)
        {
            if (null == item) throw new ArgumentNullException(nameof(item));
            this.ItemsList.AddLast(new ObjectDataItem(item, this.Cache));
        }
    }
}