using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Adriva.Worker.Core.DynamicTrigger
{
    [Singleton(Mode = SingletonMode.Listener)]
    internal sealed class DynamicTriggerListener : IListener
    {
        private readonly ITriggeredFunctionExecutor Executor;
        private readonly IDynamicTriggerListener Listener;

        private CancellationToken CancellationToken;

        public DynamicTriggerListener(ITriggeredFunctionExecutor executor, IDynamicTriggerListener listener)
        {
            this.Listener = listener ?? throw new ArgumentNullException(nameof(listener));
            this.Executor = executor;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this.CancellationToken = cancellationToken;
            await this.Listener.StartAsync(this.OnListenerDataReady, cancellationToken);
        }

        private async Task OnListenerDataReady(object data)
        {
            TriggeredFunctionData input = new TriggeredFunctionData()
            {
                TriggerValue = new DynamicTriggerData(data)
            };

            var fr = await this.Executor.TryExecuteAsync(input, this.CancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await this.Listener.StopAsync(cancellationToken);
        }

        public void Cancel()
        {

        }

        public void Dispose()
        {
            this.Listener.Dispose();
        }
    }
}