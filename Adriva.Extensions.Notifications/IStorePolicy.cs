using System;
using System.Threading.Tasks;

namespace Adriva.Extensions.Notifications
{
    public interface IStorePolicy
    {
        TimeSpan GetEmptyWaitInterval(long nullMessageCount);

        Task<bool> HandleMessageErrorAsync(INotificationStore store);
    }
}
