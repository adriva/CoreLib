using System;

namespace Adriva.Worker.Core
{
    public sealed class QueueProcessorConfiguration
    {
        public int BatchSize { get; set; } = Environment.ProcessorCount;

        public int MaxDequeueCount { get; set; } = 1;

    }
}
