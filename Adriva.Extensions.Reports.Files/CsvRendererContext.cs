using System.IO;

namespace Adriva.Extensions.Reports.Csv
{
    public class CsvRendererContext
    {
        public Stream Output { get; set; }

        public bool HasHeaders { get; set; }
    }
}
