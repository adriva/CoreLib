using System.Threading.Tasks;

namespace Adriva.Extensions.Notifications
{
    public interface INotificationService
    {
        Task<string> AddAsync(NotificationMessage message);

        Task StartAsync();

        Task StopAsync();
    }
}
