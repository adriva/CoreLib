using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Adriva.AppInsights.Serialization.Contracts;
using Adriva.Extensions.Analytics.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adriva.Extensions.Analytics
{
    internal class ProcessorService : IHostedService, IDisposable
    {
        private readonly IQueueingService QueueingService;
        private readonly AnalyticsServerOptions Options;
        private readonly IEnumerable<AnalyticsItemPopulator> Populators;

        private readonly ILogger<ProcessorService> Logger;
        private readonly IAnalyticsStore Store;

        private readonly CancellationTokenSource CancellationTokenSource;
        private readonly WaitHandle[] ProcessorWaitHandles;

        public ProcessorService(IServiceProvider serviceProvider, IAnalyticsStore store, ILogger<ProcessorService> logger)
        {
            this.Logger = logger;
            this.QueueingService = serviceProvider.GetRequiredService<IQueueingService>();
            this.Populators = serviceProvider.GetServices<AnalyticsItemPopulator>();

            var optionsAccessor = serviceProvider.GetRequiredService<IOptions<AnalyticsServerOptions>>();
            this.Options = optionsAccessor.Value;
            this.Options.ProcessorThreadCount = Math.Min(Environment.ProcessorCount, Math.Max(this.Options.ProcessorThreadCount, 1));

            this.CancellationTokenSource = new CancellationTokenSource();
            this.ProcessorWaitHandles = new WaitHandle[this.Options.ProcessorThreadCount];

            this.Store = store;
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {

            for (int loop = 0; loop < this.Options.ProcessorThreadCount; loop++)
            {
                var waitHandle = new AutoResetEvent(false);
                this.ProcessorWaitHandles[loop] = waitHandle;
                ThreadPool.QueueUserWorkItem((state) =>
                {
                    this.ProcessQueueAsync((AutoResetEvent)state, cancellationToken);
                }, waitHandle);
            }

            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            this.QueueingService.CompleteAdding();
            this.CancellationTokenSource.Cancel();
            var initializedHandles = this.ProcessorWaitHandles.Where(x => null != x).ToArray();
            if (initializedHandles.Any()) WaitHandle.WaitAll(initializedHandles, TimeSpan.FromSeconds(30));
            await Task.CompletedTask;
        }

        private async void ProcessQueueAsync(AutoResetEvent waitHandle, CancellationToken cancellationToken)
        {

            var intervalInMsecs = this.Options.FlushInterval * 1000;

            try
            {
                if (this.QueueingService.IsAddingCompleted)
                {
                    return;
                }

                List<AnalyticsItem> items = new List<AnalyticsItem>();

                while (!this.QueueingService.IsAddingCompleted)
                {
                    while (this.QueueingService.TryGetNext(5, out Envelope envelope))
                    {
                        if (AnalyticsItemPopulator.TryPopulateItem(envelope, out AnalyticsItem analyticsItem))
                        {
                            this.Logger.LogInformation("Analytics item populated from the envelope.");

                            var matchingPopulator = this.Populators.FirstOrDefault(p => 0 == string.Compare(p.TargetKey, analyticsItem.Type, StringComparison.Ordinal));

                            if (null != matchingPopulator)
                            {
                                if (matchingPopulator.TryPopulate(envelope, ref analyticsItem))
                                {
                                    items.Add(analyticsItem);
                                }
                            }
                            else
                            {
                                this.Logger.LogWarning($"No populator found for '{analyticsItem?.Type}'.");
                            }
                        }

                        if (this.Options.FlushLimit <= items.Count)
                        {
                            await this.Store.StoreAsync(items);
                            items.Clear();
                        }
                    }
                }

                await this.Store.StoreAsync(items);
            }
            finally
            {
                waitHandle.Set();
            }
        }


        public void Dispose()
        {
            Array.ForEach(this.ProcessorWaitHandles, wh => { wh?.Dispose(); });
        }
    }
}