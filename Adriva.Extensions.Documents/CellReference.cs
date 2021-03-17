using System;
using System.Linq;
using System.Text;
using Adriva.Common.Core;
using DocumentFormat.OpenXml;

namespace Adriva.Extensions.Documents
{
    public struct CellReference
    {
        private static readonly string ExcelColumnsAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static CellReference Empty { get; } = new CellReference();

        public string ColumnName { get; private set; }

        public uint RowIndex { get; private set; }

        public uint ColumnIndex => CellReference.GetColumnIndex(this.ColumnName);

        public static bool TryParse(string value, out CellReference cellReference)
        {
            cellReference = CellReference.Empty;

            if (string.IsNullOrWhiteSpace(value)) return false;

            value = value.ToUpperInvariant();

            if (value.Any(c => !char.IsDigit(c) && -1 == CellReference.ExcelColumnsAlphabet.IndexOf(c))) return false;

            int loop = 0;

            StringBuilder nameBuffer = new StringBuilder();
            StringBuilder indexBuffer = new StringBuilder();


            while (loop < value.Length && char.IsLetter(value[loop]))
            {
                nameBuffer.Append(value[loop]);
                ++loop;
            }

            while (loop < value.Length && char.IsDigit(value[loop]))
            {
                indexBuffer.Append(value[loop]);
                ++loop;
            }


            if (loop < value.Length) return false;

            if (0 == nameBuffer.Length || 0 == indexBuffer.Length) return false;

            cellReference = new CellReference(nameBuffer.ToString(), uint.Parse(indexBuffer.ToString()));
            return true;
        }

        public static uint GetColumnIndex(string columnName)
        {
            ulong value = 0;
            for (int loop = 0; loop < columnName.Length; loop++)
            {
                char c = char.ToUpperInvariant(columnName[columnName.Length - 1 - loop]);
                value += (ulong)((1 + CellReference.ExcelColumnsAlphabet.IndexOf(c)) * Math.Pow(26, loop));
            }

            return (uint)value;
        }

        public static string GetColumnName(uint columnIndex)
        {
            if (0 == columnIndex) return string.Empty;

            return Utilities.GetBaseString(columnIndex - 1, CellReference.ExcelColumnsAlphabet, 0);
        }

        public CellReference(string columnName, uint rowIndex)
        {
            if (string.IsNullOrWhiteSpace(columnName)) throw new ArgumentNullException(nameof(columnName));
            if (columnName.Any(c => -1 == CellReference.ExcelColumnsAlphabet.IndexOf(c))) throw new ArgumentException(nameof(columnName));

            this.ColumnName = columnName.ToUpperInvariant();
            this.RowIndex = rowIndex;
        }

        public CellReference NextCell()
        {
            uint nextColumnIndex = 1 + CellReference.GetColumnIndex(this.ColumnName);
            return new CellReference(CellReference.GetColumnName(nextColumnIndex), this.RowIndex);
        }

        public CellReference PreviousCell()
        {
            ulong prevColumnIndex = CellReference.GetColumnIndex(this.ColumnName) - 1; //this is 1 based, so no need to add 1

            if (1 > prevColumnIndex) return CellReference.Empty;

            return new CellReference(Utilities.GetBaseString(prevColumnIndex - 1, CellReference.ExcelColumnsAlphabet, 0), this.RowIndex);
        }

        public CellReference NextRow(string startColumnName = null)
        {
            if (string.IsNullOrWhiteSpace(startColumnName)) startColumnName = this.ColumnName;
            return new CellReference(startColumnName, 1 + this.RowIndex);
        }

        public CellReference PreviousRow(string startColumnName = null)
        {
            if (string.IsNullOrWhiteSpace(startColumnName)) startColumnName = this.ColumnName;
            if (1 > this.RowIndex - 1) return CellReference.Empty;
            return new CellReference(startColumnName, this.RowIndex - 1);
        }

        public static uint ColumnDiff(CellReference first, CellReference second)
        {
            uint firstIndex = first.ColumnIndex;
            uint secondIndex = second.ColumnIndex;

            return 1 + (secondIndex > firstIndex ? secondIndex - firstIndex : firstIndex - secondIndex);
        }

        public static bool operator ==(CellReference first, CellReference second)
        {
            return 0 == string.Compare(first.ColumnName, second.ColumnName, StringComparison.OrdinalIgnoreCase) && first.RowIndex == second.RowIndex;
        }

        public static bool operator !=(CellReference first, CellReference second)
        {
            return 0 != string.Compare(first.ColumnName, second.ColumnName, StringComparison.OrdinalIgnoreCase) || first.RowIndex != second.RowIndex;
        }

        public static implicit operator string(CellReference cellReference)
        {
            return $"{cellReference.ColumnName}{cellReference.RowIndex}";
        }

        public static implicit operator StringValue(CellReference cellReference)
        {
            return $"{cellReference.ColumnName}{cellReference.RowIndex}";
        }

        public static implicit operator CellReference(string value)
        {
            if (CellReference.TryParse(value, out CellReference cellReference)) return cellReference;
            throw new FormatException("Specified valud '{value ?? \"NULL\"}' is not a valid cell reference");
        }

        public override bool Equals(object obj)
        {
            if (obj is CellReference cellReference)
            {
                return 0 == string.Compare(this.ColumnName, cellReference.ColumnName, StringComparison.OrdinalIgnoreCase) && this.RowIndex == cellReference.RowIndex;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return string.Concat(this.ColumnName?.ToUpperInvariant(), this.RowIndex.ToString()).GetHashCode();
        }

        public override string ToString()
        {
            return $"CellReference, [{this.ColumnName}{this.RowIndex}]";
        }
    }
}
