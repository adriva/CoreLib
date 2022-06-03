using Adriva.AppInsights;
using Adriva.AppInsights.Serialization.Contracts;
using Adriva.Extensions.Analytics.Entities;

namespace Adriva.Extensions.Analytics
{
    internal sealed class MessageItemPopulator : AnalyticsItemPopulator
    {
        public override string TargetKey => "Message";

        public override bool TryPopulate(Envelope envelope, ref AnalyticsItem analyticsItem)
        {
            if (!(envelope.EventData is MessageData messageData)) return false;

            analyticsItem.MessageItem = new MessageItem();

            analyticsItem.MessageItem.Message = messageData.Message;
            analyticsItem.MessageItem.Severity = messageData.SeverityLevel;

            if (messageData.Properties.TryGetBoolean("DeveloperMode", out bool isDeveloperMode)) analyticsItem.MessageItem.IsDeveloperMode = isDeveloperMode;
            if (messageData.Properties.TryGetString("AspNetCoreEnvironment", out string environmentName)) analyticsItem.MessageItem.Environment = environmentName;
            if (messageData.Properties.TryGetString("CategoryName", out string categoryName)) analyticsItem.MessageItem.Category = categoryName;

            return !string.IsNullOrWhiteSpace(analyticsItem.MessageItem.Message);
        }
    }
}