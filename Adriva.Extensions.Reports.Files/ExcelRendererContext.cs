using System;
using System.IO;
using Adriva.Extensions.Documents;

namespace Adriva.Extensions.Reports.Excel
{
    public class ExcelRendererContext
    {
        public string StartingCell { get; private set; } // where to start writing

        public bool ShouldGenerateTable { get; set; }

        public bool UseFastWrite { get; set; }

        public Stream Output { get; set; }

        public ExcelRendererContext(string startingCell)
        {
            if (!CellReference.TryParse(startingCell, out _))
            {
                throw new ArgumentException($"'{startingCell ?? "NULL"}' is not a valid cell reference.");
            }

            this.StartingCell = startingCell;
        }
    }
}
