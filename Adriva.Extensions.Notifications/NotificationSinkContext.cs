namespace Adriva.Extensions.Notifications
{
    public sealed class NotificationSinkContext
    {
        public NotificationMessage Message { get; private set; }

        public bool IsStopped { get; private set; }

        internal NotificationSinkContext(NotificationMessage message)
        {
            this.Message = message;
        }

        public void StopProcessing()
        {
            this.IsStopped = true;
        }
    }
}
