using System;
using System.Threading;
using System.Threading.Tasks;

namespace Adriva.Worker.Core.DynamicTrigger
{
    public interface IDynamicTriggerListener : IDisposable
    {
        string Identifier { set; }

        Task StartAsync(Func<object, Task> dataReadyCallback, CancellationToken cancellationToken);

        Task StopAsync(CancellationToken cancellationToken);

        void Cancel();
    }
}