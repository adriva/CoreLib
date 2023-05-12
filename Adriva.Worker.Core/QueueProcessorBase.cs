using Microsoft.Azure.WebJobs.Host.Queues;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Threading;
using System.Threading.Tasks;

namespace Adriva.Worker.Core
{
    public class QueueProcessorBase : QueueProcessor
    {
        public QueueProcessorBase(QueueProcessorFactoryContext context) : base(context)
        {
        }

        protected override Task CopyMessageToPoisonQueueAsync(CloudQueueMessage message, CloudQueue poisonQueue, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

    }
}
