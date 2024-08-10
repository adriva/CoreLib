using Adriva.Common.Core;
using Adriva.Extensions.Azure;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Adriva.Worker.Core
{
    public class SystemFunctions
    {
        private readonly QueueOptions Options;

        public SystemFunctions(IOptions<QueueOptions> optionsAccessor)
        {
            this.Options = optionsAccessor.Value;
        }

        [StorageAccount("ConnectionStrings:AzureQueue")]
        [Singleton(Mode = SingletonMode.Function)]
        public async Task ProcessWorkerQueueAsync([QueueTrigger(QueueManager.WorkerQueueName)] QueueMessage queueMessage, CancellationToken cancellationToken, ILogger logger)
        {
            var processors = this.Options.GetProcessors(queueMessage.CommandType);
            IAsyncQueueProcessor currentProcessor = null;

            logger.LogInformation($"Found {processors.Count()} processors for command type '{queueMessage.CommandType}'.");

            foreach (var processor in processors)
            {
                logger.LogInformation($"Processing command '{queueMessage.CommandType}' using processor '{processor.GetType().FullName}.'");
                currentProcessor = processor;
                try
                {
                    await currentProcessor.ProcessAsync(queueMessage, cancellationToken, logger);
                    logger.LogInformation($"Finished processing command '{queueMessage.CommandType}' using processor '{processor.GetType().FullName}.'");
                }
                catch (Exception processorException)
                {
                    logger.LogError(processorException, $"Error in processor '{currentProcessor.GetType().FullName}'.");
                }
            }
        }
    }
}