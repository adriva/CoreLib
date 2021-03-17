using System.IO;

namespace Adriva.Extensions.Reports.Mvc
{
    public sealed class MvcRendererOptions
    {
        public string ViewProbePath { get; set; } = Directory.GetCurrentDirectory();

        public string DataApiUrl { get; set; }

        public string CommandApiUrl { get; set; }
    }
}