using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Adriva.Extensions.Caching
{
    public interface ISmartCache
    {
        TItem GetOrSet<TItem>(string key, Func<ICacheEntry, TItem> factory);

        TItem GetOrSet<TItem>(string key, string dependencyIdentifier, Func<ICacheEntry, TItem> factory);

        Task<TItem> GetOrSetAsync<TItem>(string key, Func<ICacheEntry, Task<TItem>> factory);

        Task<TItem> GetOrSetAsync<TItem>(string key, string dependencyIdentifier, Func<ICacheEntry, Task<TItem>> factory);

        bool Update<TItem>(string key, TItem item);

        void NotifyChanged(string dependencyIdentifier);

    }
}