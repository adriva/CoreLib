using Adriva.Common.Core;
using System;

namespace Adriva.Worker.Core
{
    public static class QueueOptionsExtensions
    {
        public static void RegisterProcessor<T>(this QueueOptions options, T enumValue, IAsyncQueueProcessor processor) where T : Enum
        {
            if (null == processor) return;
            options.AddProcessor(Convert.ToString(enumValue), processor);
        }
    }
}
