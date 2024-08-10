using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Adriva.Common.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Adriva.Extensions.Notifications
{
    public class NotificationService : INotificationService, IDisposable
    {
        private ILogger Logger;
        private readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        private ManualResetEvent StopSignal = new ManualResetEvent(false);
        private bool IsDisposed;
        private readonly INotificationStore Store;
        private readonly List<NotificationSink> Sinks;
        private readonly List<INotificationPublisher> Publishers;

        public NotificationService(IServiceProvider serviceProvider, ILogger<NotificationService> logger)
        {
            IStorePolicy storePolicy = serviceProvider.GetService<IStorePolicy>() ?? new DefaultStorePolicy();
            this.Store = ActivatorUtilities.CreateInstance<DefaultStoreWithPolicy>(serviceProvider, storePolicy);
            var sinkWrappers = serviceProvider.GetServices<NotificationSinkWrapper>();

            this.Sinks = sinkWrappers.OrderBy(x => x.Order).Select(x => x.Sink).ToList();
            this.Publishers = serviceProvider.GetServices<INotificationPublisher>().ToList();
            this.Logger = logger;
        }

        protected virtual void ValidateMessage(NotificationMessage message)
        {
            if (null == message) throw new ArgumentNullException(nameof(message));

            if (null == message.Recipients || 0 == message.Recipients.Count)
            {
                throw new ArgumentException($"A message should contain at least one recipient", nameof(message.Recipients));
            }
        }

        public async Task<string> AddAsync(NotificationMessage message)
        {
            this.ValidateMessage(message);
            this.Logger.LogInformation("Adding message to store.");
            message.Id = Utilities.GetBaseString(Guid.NewGuid().ToByteArray(), Utilities.Base63Alphabet, -1);
            await this.Store.AddAsync(message);
            this.Logger.LogInformation($"Message '{message.Id}' added to store.");
            return message.Id;
        }

        public Task StartAsync()
        {
            if (0 == this.Publishers.Count)
            {
                throw new InvalidOperationException("No notification publishers registered. Have you forgotten to call INotificationServiceBuilder.AddPublisher during startup?");
            }

            _ = this.RunLoopAsync();
            return Task.CompletedTask;
        }

        public async Task RunLoopAsync()
        {

            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            await this.Store.InitializeAsync();

            foreach (var publisher in this.Publishers)
            {
                await publisher.InitializeAsync();
            }

            while (!this.CancellationTokenSource.IsCancellationRequested)
            {
                NotificationMessage originalMessage = await this.Store.GetNextAsync(this.CancellationTokenSource.Token);
                NotificationMessage currentMessage = originalMessage;

                if (this.CancellationTokenSource.IsCancellationRequested) break;

                if (null != currentMessage)
                {
                    bool hasSinkError = false;
                    try
                    {
                        foreach (var sink in this.Sinks)
                        {
                            NotificationSinkContext context = new NotificationSinkContext(currentMessage);

                            try
                            {
                                currentMessage = await sink.ProcessAsync(context, this.CancellationTokenSource.Token);
                                if (0 != string.Compare(currentMessage.Id, originalMessage.Id, StringComparison.Ordinal))
                                {
                                    throw new InvalidOperationException("The id of the message cannot be mutated during processing. Please use the NotificationMessage.WithXXX(...) methods to alter the message state if needed.");
                                }
                                if (context.IsStopped) break;
                            }
                            catch (Exception processorError)
                            {
                                this.Logger.LogError(processorError, $"Error processing message '{originalMessage.Id}' in sink '{sink.GetType().FullName}'.");
                                hasSinkError = true;
                                break;
                            }
                            if (this.CancellationTokenSource.IsCancellationRequested) break;
                        }

                        if (this.CancellationTokenSource.IsCancellationRequested) break;

                        if (!hasSinkError && NotificationTarget.None != currentMessage.Target)
                        {
                            NotificationPublishContext publishContext = new NotificationPublishContext(currentMessage);

                            foreach (var publisher in this.Publishers)
                            {
                                try
                                {
                                    if (publisher.CanPublish(currentMessage))
                                    {
                                        await publisher.PublishAsync(publishContext, this.CancellationTokenSource.Token);
                                        if (publishContext.IsCompleted) break;
                                    }
                                }
                                catch (Exception publishError)
                                {
                                    this.Logger.LogError(publishError, $"Error occured publishing message '{originalMessage.Id}' using '{publisher.GetType().FullName}',");
                                }
                            }
                        }
                        await this.Store.DeleteAsync(originalMessage);
                    }
                    catch (Exception fatalError)
                    {
                        this.Logger.LogError(fatalError, $"Error processing notification for message '{originalMessage.Id}'.");
                    }
                }
            }

            this.StopSignal.Set();
        }

        public async Task StopAsync()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            this.CancellationTokenSource.Cancel();

            this.StopSignal.WaitOne();

            await this.Store.CloseAsync();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.IsDisposed)
            {
                if (disposing)
                {
                    this.CancellationTokenSource.Cancel();
                    this.StopSignal.WaitOne();
                    this.StopSignal.Dispose();

                    if (this.Store is IDisposable disposableStore)
                    {
                        disposableStore.Dispose();
                    }

                    foreach (var publisher in this.Publishers)
                    {
                        if (publisher is IDisposable disposablePublisher)
                        {
                            disposablePublisher.Dispose();
                        }
                    }

                    this.CancellationTokenSource.Dispose();
                }

                this.IsDisposed = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
