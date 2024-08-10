using System;
using System.Collections.ObjectModel;
using System.IO;

namespace Adriva.Extensions.Documents
{
    public interface IExcelWorkbook : IDocument, IDisposable
    {
        ReadOnlyCollection<IExcelWorksheet> Worksheets { get; }

        void Open(string path, FileAccess fileAccess);

        void Create(string path);

        void Save();

        IExcelWorksheet AddWorksheet(string name = null);
    }
}
