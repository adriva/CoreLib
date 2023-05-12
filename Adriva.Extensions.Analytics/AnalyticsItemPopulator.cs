using Adriva.AppInsights;
using Adriva.AppInsights.Serialization.Contracts;
using Adriva.Extensions.Analytics.Entities;

namespace Adriva.Extensions.Analytics
{
    public abstract class AnalyticsItemPopulator
    {
        public virtual string TargetKey { get => null; }

        public abstract bool TryPopulate(Envelope envelope, ref AnalyticsItem analyticsItem);

        internal static bool TryPopulateItem(Envelope envelope, out AnalyticsItem analyticsItem)
        {
            analyticsItem = null;

            if (null == envelope) return false;

            analyticsItem = new AnalyticsItem();

            analyticsItem.InstrumentationKey = envelope.InstrumentationKey;
            analyticsItem.Timestamp = envelope.EventDate;
            if (envelope.Tags.TryGetString("ai.location.ip", out string ip)) analyticsItem.Ip = ip;
            if (envelope.Tags.TryGetString("ai.operation.id", out string operationId)) analyticsItem.OperationId = operationId;
            if (envelope.Tags.TryGetString("ai.operation.parentId", out string parentOperationId)) analyticsItem.ParentOperationId = parentOperationId;
            if (envelope.Tags.TryGetString("ai.cloud.roleInstance", out string roleInstance)) analyticsItem.RoleInstance = roleInstance;
            if (envelope.Tags.TryGetString("ai.internal.sdkVersion", out string sdkVersion)) analyticsItem.SdkVersion = sdkVersion;
            if (envelope.Tags.TryGetString("ai.application.ver", out string appVersion)) analyticsItem.ApplicationVersion = appVersion;
            if (envelope.Tags.TryGetString("ai.user.id", out string userId)) analyticsItem.UserId = userId;
            if (envelope.Tags.TryGetString("ai.user.accountId", out string userAccountId)) analyticsItem.UserAccountId = userAccountId;
            if (envelope.Tags.TryGetString("ai.user.authUserId", out string authenticatedUserId)) analyticsItem.AuthenticatedUserId = authenticatedUserId;

            analyticsItem.Type = envelope.Name.Substring(1 + envelope.Name.LastIndexOf('.'));

            return true;
        }
    }
}