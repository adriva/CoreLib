using Adriva.AppInsights;
using Adriva.AppInsights.Serialization.Contracts;
using Adriva.Extensions.Analytics.Entities;

namespace Adriva.Extensions.Analytics
{
    internal class RequestItemPopulator : AnalyticsItemPopulator
    {
        public override string TargetKey => "Request";

        public override bool TryPopulate(Envelope envelope, ref AnalyticsItem analyticsItem)
        {
            if (!(envelope.EventData is RequestData requestData)) return false;

            analyticsItem.RequestItem = new RequestItem();

            analyticsItem.RequestItem.Name = requestData.Name;
            if (requestData.Properties.TryGetBoolean("DeveloperMode", out bool isDeveloperMode)) analyticsItem.RequestItem.IsDeveloperMode = isDeveloperMode;
            if (requestData.Properties.TryGetString("AspNetCoreEnvironment", out string environment)) analyticsItem.RequestItem.Environment = environment;
            analyticsItem.RequestItem.IsSuccess = requestData.IsSuccess;
            analyticsItem.RequestItem.Name = requestData.Name;
            analyticsItem.RequestItem.Duration = requestData.DurationInMilliseconds;

            if (int.TryParse(requestData.ResponseCode, out int responseCode)) analyticsItem.RequestItem.ResponseCode = responseCode;
            analyticsItem.RequestItem.Url = requestData.Url;
            return true;
        }
    }
}