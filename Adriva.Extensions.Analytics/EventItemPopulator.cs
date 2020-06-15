using Adriva.AppInsights;
using Adriva.AppInsights.Serialization.Contracts;
using Adriva.Extensions.Analytics.Entities;

namespace Adriva.Extensions.Analytics
{
    internal sealed class EventItemPopulator : AnalyticsItemPopulator
    {
        public override string TargetKey => "Event";

        public override bool TryPopulate(Envelope envelope, ref AnalyticsItem analyticsItem)
        {
            if (!(envelope.EventData is EventData eventData)) return false;

            EventItem eventItem = new EventItem();

            eventItem.Name = eventData.Name;
            if (eventData.Properties.TryGetBoolean("DeveloperMode", out bool isDeveloperMode)) eventItem.IsDeveloperMode = isDeveloperMode;
            if (eventData.Properties.TryGetString("AspNetCoreEnvironment", out string environment)) eventItem.Environment = environment;

            analyticsItem.Events.Add(eventItem);

            return true;
        }
    }
}