using System;
using System.Linq;
using Adriva.AppInsights;
using Adriva.AppInsights.Serialization.Contracts;
using Adriva.Extensions.Analytics.Entities;

namespace Adriva.Extensions.Analytics
{
    internal class ExceptionItemPopulator : AnalyticsItemPopulator
    {
        public override string TargetKey => "Exception";

        public override bool TryPopulate(Envelope envelope, ref AnalyticsItem analyticsItem)
        {
            if (!(envelope.EventData is ExceptionData exceptionData)) return false;
            if (null == exceptionData.Exceptions || 0 == exceptionData.Exceptions.Count) return false;

            foreach (var exceptionDetails in exceptionData.Exceptions)
            {
                ExceptionItem exceptionItem = new ExceptionItem();
                if (exceptionData.Properties.TryGetString("RequestId", out string requestId)) exceptionItem.RequestId = requestId;
                if (exceptionData.Properties.TryGetString("CategoryName", out string categoryName)) exceptionItem.Category = categoryName;
                if (exceptionData.Properties.TryGetString("RequestPath", out string path)) exceptionItem.Path = path;
                if (exceptionData.Properties.TryGetString("ConnectionId", out string connectionId)) exceptionItem.ConnectionId = connectionId;
                if (exceptionData.Properties.TryGetString("FormattedMessage", out string message)) exceptionItem.Message = message;
                if (exceptionData.Properties.TryGetString("EventName", out string eventName)) exceptionItem.Name = eventName;

                exceptionItem.ExceptionId = exceptionDetails.Id;
                exceptionItem.ExceptionMessage = exceptionDetails.Message;
                exceptionItem.ExceptionType = exceptionDetails.TypeName;

                if (null != exceptionDetails.ParsedStack && 0 < exceptionDetails.ParsedStack.Count)
                {
                    exceptionItem.StackTrace = string.Join(Environment.NewLine, exceptionDetails.ParsedStack.Select(s => s.ToString()));
                }

                analyticsItem.Exceptions.Add(exceptionItem);
            }

            return true;
        }
    }
}