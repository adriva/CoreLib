using System.Threading;
using System.Threading.Tasks;

namespace Adriva.Extensions.Notifications
{
    public interface INotificationStore
    {
        Task InitializeAsync();

        Task AddAsync(NotificationMessage message);

        Task<NotificationMessage> GetNextAsync(CancellationToken cancellationToken);

        Task DeleteAsync(NotificationMessage message);

        Task CloseAsync();
    }
}
