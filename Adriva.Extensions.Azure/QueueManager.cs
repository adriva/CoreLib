using Adriva.Common.Core;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Adriva.Extensions.Azure
{
    public class QueueManager
    {
        /*
		WorkerQ: 
			- Messages are processed in parallel. (That means at all times, there will be at least two threads processing those messages)
			- Also LockIdentifier doesn't mean anything to those messages, it is simply ignored
		BulkQ:
			- One message is processed by the system at a time on a single instance of the worker.
			- In case where there are multiple instances of the system running, LockIdentifier comes into play.
			  Such as, all workers lock wait on the LockIdentifier. If lock identifier is set to NULL (shouldn't be)
			  Then the LockIdentifier = INSTANCE_ID of the application which causes different instances to run in parallel.
			- It's a good practive to have a BatchId for bulkd messages and use that BatchId (whatever it is) to synchronize the work.
			  In such a scenario, there will be only one instance processing the messages with the same BatchId at any times.
			  (e.g. message.LockIdentifier = "UpdateData_Then_InsertNewData_Batch"
		*/
        public const string WorkerQueueName = "workerq";

        private static ConcurrentDictionary<string, Task<QueueManager>> CachedQueueManagers = new ConcurrentDictionary<string, Task<QueueManager>>();
        private static readonly int DefaultTtlInMinutes = 59;

        private CloudQueue CloudQueue;

        public string QueueName { get; private set; }

        public static async Task<QueueManager> GetWorkerAsync()
        {
            return await QueueManager.GetAsync(QueueManager.WorkerQueueName);
        }

        public static Task<QueueManager> GetAsync(string queueName)
        {
            return QueueManager.CachedQueueManagers.GetOrAdd(queueName, key =>
            {
                return QueueManager.CreateAsync(queueName);
            });
        }

        public static async Task<QueueManager> CreateAsync(string queueName)
        {
            var queueManager = new QueueManager(queueName);
            await queueManager.InitializeAsync();
            return queueManager;
        }

        public static async Task<QueueManager> CreateAsync(string queueName, string connectionString)
        {
            var queueManager = new QueueManager(queueName);
            await queueManager.InitializeAsync(connectionString);
            return queueManager;
        }

        private QueueManager(string queueName)
        {
            this.QueueName = queueName;
        }

        private async Task InitializeAsync()
        {
            await this.InitializeAsync(ConnectionStrings.Default.AzureQueue);
        }


        private async Task InitializeAsync(string connectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var queueClient = storageAccount.CreateCloudQueueClient();
            this.CloudQueue = queueClient.GetQueueReference(this.QueueName);

            await this.CloudQueue.CreateIfNotExistsAsync();
        }

        public async Task SendMessageAsync(int ttlInMinutes, int visibilityDelayInSeconds, QueueMessage message)
        {
            if (0 >= visibilityDelayInSeconds)
            {
                await this.SendMessageAsync(ttlInMinutes, message);
                return;
            }

            TimeSpan visibilityDelayTimespan = TimeSpan.FromSeconds(visibilityDelayInSeconds);
            TimeSpan ttlTimespan = TimeSpan.FromSeconds((ttlInMinutes * 60) + visibilityDelayInSeconds);
            var cloudMessage = message.ToCloudQueueMessage();
            await this.CloudQueue.AddMessageAsync(cloudMessage, ttlTimespan, visibilityDelayTimespan, null, null);
        }

        public async Task SendMessageAsync(int ttlInMinutes, QueueMessage message)
        {
            if (null == message) return;

            var cloudMessage = message.ToCloudQueueMessage();
            await this.CloudQueue.AddMessageAsync(cloudMessage, TimeSpan.FromMinutes(ttlInMinutes), null, null, null);
        }

        public async Task SendMessageAsync(QueueMessage message)
        {
            await this.SendMessageAsync(QueueManager.DefaultTtlInMinutes, message);
        }

        public async Task SendMessageAsync(int ttlInMinutes, int visibilityDelayInSeconds, string messageText)
        {
            if (0 >= visibilityDelayInSeconds)
            {
                await this.SendMessageAsync(ttlInMinutes, messageText);
                return;
            }

            CloudQueueMessage cloudQueueMessage = new CloudQueueMessage(messageText);
            TimeSpan visibilityDelay = TimeSpan.FromSeconds(visibilityDelayInSeconds);
            TimeSpan ttlTimespan = TimeSpan.FromSeconds((ttlInMinutes * 60) + visibilityDelayInSeconds);
            await this.CloudQueue.AddMessageAsync(cloudQueueMessage, ttlTimespan, visibilityDelay, null, null);
        }

        public async Task SendMessageAsync(int ttlInMinutes, string messageText)
        {
            CloudQueueMessage cloudQueueMessage = new CloudQueueMessage(messageText);
            await this.CloudQueue.AddMessageAsync(cloudQueueMessage, TimeSpan.FromMinutes(ttlInMinutes), null, null, null);
        }

        public async Task SendMessageAsync(string messageText)
        {
            await this.SendMessageAsync(QueueManager.DefaultTtlInMinutes, messageText);
        }

        public async Task<string> GetStringAsync()
        {
            var cloudMessage = await this.CloudQueue.GetMessageAsync();

            if (null == cloudMessage) return null;

            string messageText = cloudMessage.AsString;

            await this.CloudQueue.DeleteMessageAsync(cloudMessage);

            return messageText;
        }

        public async Task<T> GetJsonObjectAsync<T>() where T : class
        {
            var jsonMessage = await this.GetStringAsync();

            if (null == jsonMessage) return null;

            return Utilities.SafeDeserialize<T>(jsonMessage);
        }
    }
}
