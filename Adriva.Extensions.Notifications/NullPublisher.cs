using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Adriva.Extensions.Notifications
{
    public sealed class NullPublisher : INotificationPublisher
    {
        private readonly ILogger Logger;

        public NullPublisher(ILogger<NullPublisher> logger)
        {
            this.Logger = logger;
        }

        public bool CanPublish(NotificationMessage message) => true;

        public Task InitializeAsync() => Task.CompletedTask;

        public Task PublishAsync(NotificationPublishContext context, CancellationToken cancellationToken)
        {
            this.Logger.LogInformation($"Message '{context.Message.Id}' published using NullPublisher.");
            return Task.CompletedTask;
        }
    }
}
