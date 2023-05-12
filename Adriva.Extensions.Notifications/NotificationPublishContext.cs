namespace Adriva.Extensions.Notifications
{
    public sealed class NotificationPublishContext
    {
        public NotificationMessage Message { get; private set; }

        public bool IsCompleted { get; private set; }

        public NotificationPublishContext(NotificationMessage message)
        {
            this.Message = message;
        }

        public void SetComplete()
        {
            this.IsCompleted = true;
        }
    }
}
