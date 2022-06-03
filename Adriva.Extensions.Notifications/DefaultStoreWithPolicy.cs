using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Adriva.Extensions.Notifications
{
    internal sealed class DefaultStoreWithPolicy : INotificationStore, IDisposable
    {
        private readonly ILogger Logger;
        private readonly INotificationStore Store;
        private readonly IStorePolicy Policy;

        private long ConsecutiveNullMessageCount = 0;

        public DefaultStoreWithPolicy(INotificationStore store, IStorePolicy policy, ILogger<DefaultStoreWithPolicy> logger)
        {
            this.Store = store;
            this.Policy = policy;
            this.Logger = logger;
        }

        public Task AddAsync(NotificationMessage message)
        {
            return this.Store.AddAsync(message);
        }

        public Task CloseAsync()
        {
            return this.Store.CloseAsync();
        }

        public async Task<NotificationMessage> GetNextAsync(CancellationToken cancellationToken)
        {
            NotificationMessage notificationMessage = null;

            try
            {
                notificationMessage = await this.Store.GetNextAsync(cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }
            }
            catch (Exception error)
            {
                if (!await this.Policy.HandleMessageErrorAsync(this.Store))
                {
                    throw error;
                }
                else
                {
                    this.Logger.LogWarning(error, "Error occured in notification store but handled by the policy. The error is ignored.");
                }
            }

            if (null == notificationMessage)
            {
                Interlocked.Increment(ref this.ConsecutiveNullMessageCount);
                TimeSpan waitTime = this.Policy.GetEmptyWaitInterval(this.ConsecutiveNullMessageCount);

                if (TimeSpan.Zero < waitTime)
                {
                    this.Logger.LogDebug($"Waiting for {waitTime} according to the store policy.");
                    await Task.Delay(waitTime);
                }
            }
            else
            {
                this.ConsecutiveNullMessageCount = 0;
                return notificationMessage;
            }

            return notificationMessage;
        }

        public Task DeleteAsync(NotificationMessage message)
        {
            return this.Store.DeleteAsync(message);
        }

        public Task InitializeAsync()
        {
            return this.Store.InitializeAsync();
        }

        public void Dispose()
        {
            if (this.Store is IDisposable disposableStore)
            {
                disposableStore.Dispose();
            }
        }
    }
}
