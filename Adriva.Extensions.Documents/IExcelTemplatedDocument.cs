using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Adriva.Extensions.Documents
{
    public interface IExcelTemplatedDocument : IDocument, IDisposable
    {
        Task<string> PopulateTemplateAsync(string worksheetName, CellReference startingCell, IEnumerable<object[]> dataRows, bool shouldGenerateTable = false);
    }
}