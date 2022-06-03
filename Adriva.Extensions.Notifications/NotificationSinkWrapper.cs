namespace Adriva.Extensions.Notifications
{
    internal sealed class NotificationSinkWrapper
    {
        public int Order { get; private set; }

        public NotificationSink Sink { get; private set; }

        public NotificationSinkWrapper(NotificationSink sink, int order)
        {
            this.Sink = sink;
            this.Order = order;
        }

        public static implicit operator NotificationSink(NotificationSinkWrapper wrapper)
        {
            return wrapper.Sink;
        }
    }
}
