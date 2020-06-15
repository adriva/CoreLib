using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Adriva.Extensions.Reports
{

    internal sealed class NullMemoryCache : IMemoryCache
    {
        private class NullCacheEntry : ICacheEntry
        {
            public object Key => null;
            public object Value { get => null; set { } }
            public DateTimeOffset? AbsoluteExpiration { get => null; set { } }
            public TimeSpan? AbsoluteExpirationRelativeToNow { get => null; set { } }
            public TimeSpan? SlidingExpiration { get => null; set { } }
            public IList<IChangeToken> ExpirationTokens => Array.Empty<IChangeToken>();
            public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks => Array.Empty<PostEvictionCallbackRegistration>();
            public CacheItemPriority Priority { get => CacheItemPriority.Normal; set { } }
            public long? Size { get => 0; set { } }

            public void Dispose()
            {

            }
        }

        public ICacheEntry CreateEntry(object key)
        {
            return new NullCacheEntry();
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