using System.Threading;
using System.Threading.Tasks;

namespace Adriva.Extensions.Notifications
{
    public interface INotificationPublisher
    {
        bool CanPublish(NotificationMessage message);

        Task InitializeAsync();

        Task PublishAsync(NotificationPublishContext context, CancellationToken cancellationToken);
    }
}
