using System.Collections.Concurrent;
using Adriva.AppInsights.Serialization.Contracts;
using Microsoft.Extensions.Logging;

namespace Adriva.Extensions.Analytics
{
    internal sealed class QueueingService : IQueueingService
    {

        private readonly BlockingCollection<Envelope> Items = new BlockingCollection<Envelope>();
        private readonly ILogger<QueueingService> Logger;

        public bool IsAddingCompleted => this.Items.IsAddingCompleted;

        public QueueingService(ILogger<QueueingService> logger)
        {
            this.Logger = logger;
        }

        public void CompleteAdding()
        {
            this.Items.CompleteAdding();
        }

        public void Queue(Envelope envelope)
        {
            if (null == envelope) return;
            if (this.Items.IsCompleted) return;

            if (this.Items.TryAdd(envelope, 50))
            {
                this.Logger.LogTrace("Queued envelope item");
            }
        }

        public bool TryGetNext(int millisecondsTimeout, out Envelope envelope)
        {
            return this.Items.TryTake(out envelope, millisecondsTimeout);
        }
    }
}