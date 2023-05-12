using System;

namespace Adriva.Extensions.Reports
{
    public class QueryDefinition
    {
        public string Command { get; set; }

        public TimeSpan? SlidingExpiration { get; set; }

        public TimeSpan? AbsoluteExpiration { get; set; }

        public override string ToString()
        {
            return $"[Query] Command = '{this.Command}'";
        }
    }


}