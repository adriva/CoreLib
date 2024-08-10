using System.Collections.Generic;

namespace Adriva.Extensions.Reports.Mvc
{
    public class MvcFilterRendererOptions
    {
        public string View { get; set; }

        public Dictionary<string, string> Properties { get; set; }
    }
}