namespace Adriva.Extensions.Documents
{
    public interface IDocumentManager
    {
        IDocument<ExcelSheetRow> GetExcel();

        T Get<T>() where T : class, IDocument;
    }
}