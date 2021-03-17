using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Adriva.Extensions.Notifications
{
    public class DefaultStorePolicy : IStorePolicy
    {
        public virtual TimeSpan GetEmptyWaitInterval(long nullMessageCount)
        {
            if (0 == nullMessageCount) return TimeSpan.Zero;
            nullMessageCount = Math.Min(10, nullMessageCount);

            long waitMillisconds = (long)Math.Pow(nullMessageCount, 2) * 100;

            return TimeSpan.FromMilliseconds(Math.Min(waitMillisconds, 10_000L));
        }

        public virtual Task<bool> HandleMessageErrorAsync(INotificationStore store) => Task.FromResult(false);
    }
}
