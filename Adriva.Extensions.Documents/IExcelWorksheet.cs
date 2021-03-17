namespace Adriva.Extensions.Documents
{
    public interface IExcelWorksheet
    {
        string Name { get; set; }

        string Dimension { get; }

        void FastWriteRow(CellReference startingCell, object[] row, bool applyDefaultDataStyle = true);

        void Write(CellReference cellReference, object value, bool applyDefaultDataStyle = true);

        bool TryRead(CellReference cellReference, out object value);

        void CreateTable(CellReference startCell, CellReference endCell);

        bool TryGetCellStyleIndex(CellReference cellReference, out uint styleIndex);

        void CopyCellStyle(CellReference fromCell, CellReference toCell);

        void ApplyCellStyle(uint styleIndex, CellReference cell);

        void Save();
    }
}
