using System;
using System.Linq;
using Adriva.AppInsights;
using Adriva.AppInsights.Serialization.Contracts;

namespace Adriva.Extensions.Analytics.Entities
{
    public class ExceptionItem
    {
        public long Id { get; set; }

        public long AnalyticsItemId { get; set; }

        public string Name { get; set; }

        public string RequestId { get; set; }

        public string Category { get; set; }

        public string ConnectionId { get; set; }

        public string Message { get; set; }

        public string Path { get; set; }

        public string ExceptionType { get; set; }

        public int ExceptionId { get; set; }

        public string ExceptionMessage { get; set; }

        public string StackTrace { get; set; }

        public AnalyticsItem AnalyticsItem { get; set; }

    }
}