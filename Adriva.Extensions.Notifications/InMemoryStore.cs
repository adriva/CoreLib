using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Adriva.Extensions.Notifications
{
    public sealed class InMemoryStore : INotificationStore
    {
        private readonly ConcurrentQueue<NotificationMessage> NormalQueue = new ConcurrentQueue<NotificationMessage>();
        private readonly ConcurrentQueue<NotificationMessage> LowQueue = new ConcurrentQueue<NotificationMessage>();
        private readonly ConcurrentQueue<NotificationMessage> HighQueue = new ConcurrentQueue<NotificationMessage>();

        public Task InitializeAsync() => Task.CompletedTask;

        public Task AddAsync(NotificationMessage message)
        {
            ConcurrentQueue<NotificationMessage> queue = null;

            switch (message.Priority)
            {
                case Priority.Low:
                    queue = this.LowQueue;
                    break;
                case Priority.Default:
                    queue = this.NormalQueue;
                    break;
                case Priority.High:
                    queue = this.HighQueue;
                    break;
            }

            queue.Enqueue(message);
            return Task.CompletedTask;
        }

        public Task<NotificationMessage> GetNextAsync(CancellationToken cancellationToken)
        {
            if (!this.HighQueue.TryDequeue(out NotificationMessage message))
            {
                if (!this.NormalQueue.TryDequeue(out message))
                {
                    if (!this.LowQueue.TryDequeue(out message))
                    {
                        message = null;
                    }
                }
            }

            return Task.FromResult(message);
        }

        public Task DeleteAsync(NotificationMessage message) => Task.CompletedTask;

        public Task CloseAsync() => Task.CompletedTask;

    }
}
