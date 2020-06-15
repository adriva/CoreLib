namespace Adriva.Extensions.Documents
{
    public sealed class ExcelSheetRow
    {
        public object[] Items { get; private set; }

        public ExcelSheetRow(params object[] items)
        {
            this.Items = items;
        }
    }
}
