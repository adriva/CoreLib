using Adriva.Common.Core;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Adriva.Extensions.Azure
{
    public static class AzureExtensions
    {
        public static CloudQueueMessage ToCloudQueueMessage(this QueueMessage queueMessage)
        {
            string json = Utilities.SafeSerialize(queueMessage);
            return new CloudQueueMessage(json);
        }
    }
}
