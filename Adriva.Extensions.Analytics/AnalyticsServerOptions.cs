using System;

namespace Adriva.Extensions.Analytics
{
    public class AnalyticsServerOptions
    {
        private int flushInterval = 60000;
        private int flushLimit = 5000;

        public string Endpoint { get; set; } = "/analytics/track";

        public string ConnectionString { get; set; }

        public int ProcessorThreadCount { get; set; } = 1;

        public bool EnableRequestLogging { get; set; }

        /// <summary>
        /// Gets or sets the flush interval in seconds
        /// </summary>
        public int FlushInterval
        {
            get => this.flushInterval;
            set => this.flushInterval = Math.Max(60, value);
        }

        public int FlushLimit
        {
            get => this.flushLimit;
            set => this.flushLimit = Math.Max(1, value);
        }

    }
}