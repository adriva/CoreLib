using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Adriva.Common.Core
{
    public interface IAsyncQueueProcessor
    {
        Task ProcessAsync(QueueMessage message, CancellationToken cancellationToken, ILogger logger);
    }

    public abstract class AsyncQueueProcessor<TData> : IAsyncQueueProcessor
    {
        protected abstract Task ProcessAsync(TData data, CancellationToken cancellationToken, ILogger logger);

        public async Task ProcessAsync(QueueMessage message, CancellationToken cancellationToken, ILogger logger)
        {
            if (null == message) return;

            TData data = Utilities.CastObject<TData>(message.Data);
            await this.ProcessAsync(data, cancellationToken, logger);
        }
    }
}
