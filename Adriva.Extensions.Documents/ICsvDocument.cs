using System;
using System.Threading.Tasks;

namespace Adriva.Extensions.Documents
{
    public interface ICsvDocument : IDocument, IDisposable
    {
        Task WriteRowAsync(object[] row);
    }
}
