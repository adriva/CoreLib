using Microsoft.Azure.WebJobs.Host.Queues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Adriva.Worker.Core
{
    public class QueueProcessorFactoryBase : IQueueProcessorFactory
    {
        protected IServiceProvider ServiceProvider { get; private set; }
        private readonly QueueOptions Options;

        public QueueProcessorFactoryBase(IServiceProvider serviceProvider, IOptions<QueueOptions> optionsAccessor)
        {
            this.ServiceProvider = serviceProvider;
            this.Options = optionsAccessor.Value;
        }

        protected virtual QueueProcessor CreateQueueProcessor(QueueProcessorFactoryContext context, QueueProcessorConfiguration queueProcessorConfiguration)
        {
            context.MaxDequeueCount = queueProcessorConfiguration.MaxDequeueCount;
            context.BatchSize = queueProcessorConfiguration.BatchSize;
            
            return ActivatorUtilities.CreateInstance<QueueProcessorBase>(this.ServiceProvider, context);
        }

        public QueueProcessor Create(QueueProcessorFactoryContext context)
        {
            var queueConfiguration = this.Options[context.Queue.Name];
            return this.CreateQueueProcessor(context, queueConfiguration);
        }
    }
}
