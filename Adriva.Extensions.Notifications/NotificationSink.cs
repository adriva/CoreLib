using System.Threading;
using System.Threading.Tasks;

namespace Adriva.Extensions.Notifications
{
    public class NotificationSink
    {
        public virtual NotificationMessage Process(NotificationSinkContext context)
        {
            return context.Message;
        }

        public virtual Task<NotificationMessage> ProcessAsync(NotificationSinkContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.Process(context));
        }
    }
}
