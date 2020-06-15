using Adriva.Common.Core;
using System;
using System.Collections.Generic;

namespace Adriva.Worker.Core
{
    public sealed class QueueOptions
    {
        private readonly Dictionary<string, QueueProcessorConfiguration> Configurations = new Dictionary<string, QueueProcessorConfiguration>();

        private readonly Dictionary<string, List<IAsyncQueueProcessor>> Processors = new Dictionary<string, List<IAsyncQueueProcessor>>();

        public QueueProcessorConfiguration this[string queueName]
        {
            get
            {
                if (!this.Configurations.ContainsKey(queueName)) return new QueueProcessorConfiguration();
                return this.Configurations[queueName];
            }
        }

        public void AddConfiguration(string queueName, QueueProcessorConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(queueName)) throw new ArgumentNullException(nameof(queueName));
            this.Configurations[queueName] = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void AddProcessor(string commandName, IAsyncQueueProcessor processor)
        {
            if (string.IsNullOrWhiteSpace(commandName)) throw new ArgumentNullException(nameof(commandName));
            if (null == processor) throw new ArgumentNullException(nameof(processor));

            if (!this.Processors.ContainsKey(commandName)) this.Processors[commandName] = new List<IAsyncQueueProcessor>();

            this.Processors[commandName].Add(processor);
        }

        public IEnumerable<IAsyncQueueProcessor> GetProcessors(string commandName)
        {
            if (string.IsNullOrEmpty(commandName)) return new IAsyncQueueProcessor[0];
            if (!this.Processors.ContainsKey(commandName)) return new IAsyncQueueProcessor[0];

            return this.Processors[commandName];
        }
    }
}
