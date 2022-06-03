using Microsoft.Extensions.Primitives;
using System;
using System.Threading.Tasks;

namespace Adriva.Extensions.Caching
{
    public class SmartCacheChangeToken : IChangeToken
    {
        public bool HasChanged { get; private set; }

        public bool ActiveChangeCallbacks => false;

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            throw new NotImplementedException();
        }

        public virtual Task<bool> CheckExpiredAsync()
        {
            return Task.FromResult(false);
        }

        internal void NotifyExpired()
        {
            if (!this.HasChanged) this.HasChanged = true;
        }
    }
}
