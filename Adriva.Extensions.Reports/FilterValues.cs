using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Adriva.Extensions.Reports
{
    public sealed class FilterValues : IDictionary<string, string>
    {
        private readonly Dictionary<string, string> Dictionary = new Dictionary<string, string>();

        private static string NormalizeKey(string key)
        {
            if (null == key) return string.Empty;
            return key.ToUpperInvariant();
        }

        public string this[string key]
        {
            get => this.Dictionary[FilterValues.NormalizeKey(key)];
            set => this.Dictionary[FilterValues.NormalizeKey(key)] = value;
        }

        public ICollection<string> Keys => this.Dictionary.Keys;

        public ICollection<string> Values => this.Dictionary.Values;

        public int Count => this.Dictionary.Count;

        public bool IsReadOnly => false;

        public void Add(string key, string value)
        {
            this.Dictionary.Add(FilterValues.NormalizeKey(key), value);
        }

        public void Add(KeyValuePair<string, string> item)
        {
            this.Dictionary.Add(FilterValues.NormalizeKey(item.Key), item.Value);
        }

        public void Clear()
        {
            this.Dictionary.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item) => this.Dictionary.Contains(new KeyValuePair<string, string>(FilterValues.NormalizeKey(item.Key), item.Value));


        public bool ContainsKey(string key) => this.Dictionary.ContainsKey(FilterValues.NormalizeKey(key));


        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            this.ToArray().CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => this.Dictionary.GetEnumerator();

        public bool Remove(string key)
        {
            return this.Dictionary.Remove(FilterValues.NormalizeKey(key));
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            string normalizedKey = FilterValues.NormalizeKey(item.Key);
            if (!this.TryGetValue(normalizedKey, out string value)) return false;
            if (0 != string.Compare(value, item.Value, StringComparison.CurrentCulture)) return false;
            return this.Remove(normalizedKey);
        }

        public bool TryGetValue(string key, out string value)
        {
            key = FilterValues.NormalizeKey(key);
            return this.Dictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator() => this.Dictionary.GetEnumerator();
    }
}