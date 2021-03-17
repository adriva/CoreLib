using Adriva.Common.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Adriva.Extensions.Caching
{
    public class SmartCache<TChangeToken> : ISmartCache where TChangeToken : SmartCacheChangeToken, new()
    {
        private readonly IMemoryCache MemoryCache;

        protected ILogger Logger { get; private set; }

        public SmartCache(IOptions<MemoryCacheOptions> optionsAccessor, ILoggerFactory loggerFactory)
        {
            this.Logger = loggerFactory.CreateLogger("Adriva.Extensions.Caching.SmartCache");
            this.MemoryCache = new MemoryCache(optionsAccessor.Value);
        }

        protected virtual string GetDependencyKey(string dependencyIdentifier)
        {
            return $"!{Utilities.CalculateHash(dependencyIdentifier)}";
        }

        public TItem GetOrSet<TItem>(string key, Func<ICacheEntry, TItem> factory)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Invalid key value.");
            if (key.StartsWith("!", StringComparison.Ordinal)) throw new ArgumentException("Invalid key value.");

            return this.MemoryCache.GetOrCreate<TItem>(key, factory);
        }

        public TItem GetOrSet<TItem>(string key, string dependencyIdentifier, Func<ICacheEntry, TItem> factory)
        {
            string dependencyKey = this.GetDependencyKey(dependencyIdentifier);

            if (this.MemoryCache.TryGetValue<TChangeToken>(dependencyKey, out TChangeToken existingChangeToken))
            {
                if (existingChangeToken.CheckExpiredAsync().Result)
                {
                    this.Logger.LogTrace($"Dependency token {dependencyIdentifier} has changed sending notification.");
                    this.MemoryCache.Remove(dependencyKey);
                    existingChangeToken.NotifyExpired();
                }
            }

            TItem item = this.GetOrSet<TItem>(key, (entry) =>
            {
                this.Logger.LogTrace($"Creating cache entry for {key}.");
                TChangeToken changeToken = new TChangeToken();
                entry.AddExpirationToken(changeToken);

                TItem cacheItem = factory.Invoke(entry);

                MemoryCacheEntryOptions cachedTokenOptions = new MemoryCacheEntryOptions()
                {
                    AbsoluteExpiration = entry.AbsoluteExpiration,
                    AbsoluteExpirationRelativeToNow = entry.AbsoluteExpirationRelativeToNow,
                    Priority = entry.Priority,
                    SlidingExpiration = entry.SlidingExpiration
                };

                cachedTokenOptions.RegisterPostEvictionCallback(this.OnPostEviction, null);

                this.MemoryCache.Set(dependencyKey, changeToken, cachedTokenOptions);

                return cacheItem;
            });

            return item;
        }

        public async Task<TItem> GetOrSetAsync<TItem>(string key, Func<ICacheEntry, Task<TItem>> factory)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Invalid key value.");
            if (key.StartsWith("!", StringComparison.Ordinal)) throw new ArgumentException("Invalid key value.");

            return await this.MemoryCache.GetOrCreateAsync<TItem>(key, factory);
        }

        private void OnPostEviction(object key, object value, EvictionReason reason, object state)
        {
            if (!(value is TChangeToken changeToken)) return;

            changeToken.NotifyExpired();
        }

        public async Task<TItem> GetOrSetAsync<TItem>(string key, string dependencyIdentifier, Func<ICacheEntry, Task<TItem>> factory)
        {
            string dependencyKey = this.GetDependencyKey(dependencyIdentifier);

            if (this.MemoryCache.TryGetValue<TChangeToken>(dependencyKey, out TChangeToken existingChangeToken))
            {
                if (await existingChangeToken.CheckExpiredAsync())
                {
                    this.Logger.LogTrace($"Dependency token {dependencyIdentifier} has changed sending notification.");
                    this.MemoryCache.Remove(dependencyKey);
                    existingChangeToken.NotifyExpired();
                }
            }

            TItem item = await this.GetOrSetAsync<TItem>(key, async (entry) =>
            {
                this.Logger.LogTrace($"Creating cache entry for {key}.");
                TChangeToken changeToken = new TChangeToken();
                entry.AddExpirationToken(changeToken);

                TItem cacheItem = await factory.Invoke(entry);

                MemoryCacheEntryOptions cachedTokenOptions = new MemoryCacheEntryOptions()
                {
                    AbsoluteExpiration = entry.AbsoluteExpiration,
                    AbsoluteExpirationRelativeToNow = entry.AbsoluteExpirationRelativeToNow,
                    Priority = entry.Priority,
                    SlidingExpiration = entry.SlidingExpiration
                };

                cachedTokenOptions.RegisterPostEvictionCallback(this.OnPostEviction, null);

                this.MemoryCache.Set(dependencyKey, changeToken, cachedTokenOptions);

                return cacheItem;
            });

            return item;
        }

        public bool Update<TItem>(string key, TItem item)
        {
            if (this.MemoryCache.TryGetValue(key, out object cachedItem))
            {
                this.MemoryCache.Set(key, item);
                return true;
            }

            return false;
        }

        public void NotifyChanged(string dependencyIdentifier)
        {
            this.Logger.LogTrace($"Sending change notification to dependency token {dependencyIdentifier}.");

            string dependencyKey = this.GetDependencyKey(dependencyIdentifier);

            if (this.MemoryCache.TryGetValue<TChangeToken>(dependencyKey, out TChangeToken changeToken))
            {
                this.MemoryCache.Remove(dependencyKey);
                this.Logger.LogTrace($"Notifying expired dependency '{dependencyKey}'.");
                changeToken.NotifyExpired();
                this.Logger.LogTrace($"Dependency token {dependencyIdentifier} removed and expired.");
            }
        }

    }
}
