using System;
using System.Drawing;
using System.Linq;

namespace Adriva.Extensions.Documents
{
    public struct CellRange
    {

        public static CellRange Empty { get; } = new CellRange();

        public CellReference Start { get; private set; }
        public CellReference End { get; private set; }

        public static bool TryParse(string rangeString, out CellRange range)
        {
            range = CellRange.Empty;

            if (string.IsNullOrWhiteSpace(rangeString) || 1 != rangeString.Count(c => ':' == c)) return false;

            string[] cellParts = rangeString.Split(':');

            if (!CellReference.TryParse(cellParts[0], out CellReference start)) return false;
            if (!CellReference.TryParse(cellParts[1], out CellReference end)) return false;

            try
            {
                range = new CellRange(start, end);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public CellRange(CellReference start, CellReference end)
        {
            if (
                CellReference.GetColumnIndex(start) > CellReference.GetColumnIndex(end)
                || start.RowIndex > end.RowIndex
            )
            {
                throw new ArgumentException("Given cells doesn't define a valid region.");
            }

            this.Start = start;
            this.End = end;
        }

        public bool Contains(CellReference cell)
        {
            uint cellColumnIndex = CellReference.GetColumnIndex(cell);
            uint startColumnIndex = CellReference.GetColumnIndex(this.Start);
            uint endColumnIndex = CellReference.GetColumnIndex(this.End);

            return startColumnIndex <= cellColumnIndex && endColumnIndex >= cellColumnIndex
                    && this.Start.RowIndex <= cell.RowIndex && this.End.RowIndex >= cell.RowIndex;
        }

        public bool TryGetIntersection(CellRange range, out CellRange intersectionRange)
        {
            intersectionRange = CellRange.Empty;

            uint startColumnIndex = Math.Max(this.Start.ColumnIndex, range.Start.ColumnIndex);
            uint endColumnIndex = Math.Min(this.Start.ColumnIndex + CellReference.ColumnDiff(this.End, this.Start), range.Start.ColumnIndex + CellReference.ColumnDiff(range.End, range.Start)) - 1;
            uint startRowIndex = Math.Max(this.Start.RowIndex, range.Start.RowIndex);
            uint endRowIndex = Math.Min(this.Start.RowIndex + 1 + (this.End.RowIndex - this.Start.RowIndex), range.Start.RowIndex + 1 + (range.End.RowIndex - range.Start.RowIndex)) - 1;

            if (endColumnIndex >= startColumnIndex && endRowIndex >= startRowIndex)
            {
                intersectionRange = new CellRange(
                        new CellReference(CellReference.GetColumnName(startColumnIndex), startRowIndex),
                        new CellReference(CellReference.GetColumnName(endColumnIndex), endRowIndex)
                );
                return true;
            }

            return false;
        }

        public static implicit operator string(CellRange range)
        {
            return $"{range.Start.ColumnName}{range.Start.RowIndex}:{range.End.ColumnName}{range.End.RowIndex}";
        }

        public static explicit operator CellRange(string rangeString)
        {
            if (CellRange.TryParse(rangeString, out CellRange range)) return range;
            throw new InvalidCastException($"'{rangeString ?? "NULL"}' doesn't represent a valid range.");
        }
    }
}
