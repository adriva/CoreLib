using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Adriva.Extensions.RuleEngine
{
    [Serializable]
    public sealed class TagsetCollection<TItem>
    {
        private readonly Dictionary<string, Tagset<TItem>> TagsetLookup = new Dictionary<string, Tagset<TItem>>();

        public ReadOnlyCollection<Tagset<TItem>> Tagsets
        {
            get => new ReadOnlyCollection<Tagset<TItem>>(this.TagsetLookup.Values.ToList());
        }

        public void AddItem(string tag, TItem item)
        {
            if (!this.TagsetLookup.ContainsKey(tag)) this.TagsetLookup[tag] = new Tagset<TItem>(tag);

            if (this.TagsetLookup[tag].ItemList.Any(x => object.ReferenceEquals(x, item))) return;

            this.TagsetLookup[tag].ItemList.Add(item);
        }
    }
}
