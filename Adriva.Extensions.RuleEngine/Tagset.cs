using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Adriva.Extensions.RuleEngine
{
    [Serializable]
    public sealed class Tagset<TItem>
    {
        internal readonly List<TItem> ItemList = new List<TItem>();

        public string Tag { get; private set; }

        public ReadOnlyCollection<TItem> Items => new ReadOnlyCollection<TItem>(this.ItemList);

        public Tagset(string tag)
        {
            this.Tag = tag;
        }

    }
}
