using System;

namespace Adriva.Extensions.Notifications
{
    [Flags]
    public enum NotificationTarget : int
    {
        None = 0,
        EMail = 1 << 0,
        Sms = 1 << 1,
        MobilePush = 1 << 2,
        WebPush = 1 << 3,
        Custom = 1 << 4
    }

    public enum Priority : int
    {
        Default = 0,
        Low = 1,
        High = 2
    }
}
