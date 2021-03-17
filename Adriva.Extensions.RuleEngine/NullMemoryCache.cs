using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Adriva.Extensions.RuleEngine
{
    internal sealed class NullMemoryCache : IMemoryCache
    {
        private sealed class NullCacheEntry : ICacheEntry
        {
            public object Key { get; set; }

            public object Value { get; set; }

            public DateTimeOffset? AbsoluteExpiration { get => null; set { } }

            public TimeSpan? AbsoluteExpirationRelativeToNow { get => null; set { } }

            public TimeSpan? SlidingExpiration { get => null; set { } }

            public IList<IChangeToken> ExpirationTokens => new List<IChangeToken>();

            public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks => new List<PostEvictionCallbackRegistration>();

            public CacheItemPriority Priority
            {
                get => CacheItemPriority.Normal;
                set { }
            }

            public long? Size
            {
                get => null;
                set { }
            }

            public NullCacheEntry(object key, object value)
            {
                this.Key = key;
                this.Value = value;
            }

            public void Dispose()
            {

            }
        }

        public ICacheEntry CreateEntry(object key)
        {
            return new NullCacheEntry(key, null);
        }

        public void Dispose()
        {

        }

        public void Remove(object key)
        {

        }

        public bool TryGetValue(object key, out object value)
        {
            value = null;
            return false;
        }
    }
}
