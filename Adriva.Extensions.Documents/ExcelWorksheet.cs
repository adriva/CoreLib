using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Adriva.Extensions.Documents
{

    internal sealed class ExcelWorksheet : IExcelWorksheet
    {
        private readonly WorkbookPart WorkbookPart;
        private readonly WorksheetPart WorksheetPart;
        private readonly Dictionary<TypeCode, uint> CellFormatIndices = new Dictionary<TypeCode, uint>();

        private SheetData CurrentSheetData = null;

        public string Name
        {
            get => this.Sheet.Name;
            set => this.Sheet.Name = value;
        }

        public string Dimension => this.WorksheetPart?.Worksheet?.SheetDimension?.Reference;

        private Sheet Sheet
        {
            get
            {
                string worksheetId = this.WorkbookPart.GetIdOfPart(this.WorksheetPart);
                var sheets = this.WorkbookPart.Workbook.GetFirstChild<Sheets>();
                return sheets.ChildElements.OfType<Sheet>().First(s => s.Id == worksheetId);
            }
        }

        private SheetData SheetData
        {
            get
            {
                if (null == this.CurrentSheetData)
                {
                    this.CurrentSheetData = this.WorksheetPart.Worksheet.GetFirstChild<SheetData>();
                }
                return this.CurrentSheetData;
            }
        }

        public ExcelWorksheet(WorkbookPart workbookPart, WorksheetPart worksheetPart)
        {
            this.WorkbookPart = workbookPart;
            this.WorksheetPart = worksheetPart;
            this.GenerateStyleSheet();
        }

        private void GenerateStyleSheet()
        {
            var stylesPart = this.WorkbookPart.GetPartsOfType<WorkbookStylesPart>().FirstOrDefault();

            if (null == stylesPart)
            {
                stylesPart = this.WorkbookPart.AddNewPart<WorkbookStylesPart>();
            }

            if (null == stylesPart.Stylesheet)
            {
                stylesPart.Stylesheet = new Stylesheet();

                stylesPart.Stylesheet.Fonts = new Fonts();
                stylesPart.Stylesheet.Fonts.AppendChild(new Font() { });
                stylesPart.Stylesheet.Fonts.Count = 1;

                stylesPart.Stylesheet.Fills = new Fills();
                stylesPart.Stylesheet.Fills.AppendChild(new Fill { PatternFill = new PatternFill { PatternType = PatternValues.None } }); // required, reserved by Excel
                stylesPart.Stylesheet.Fills.AppendChild(new Fill { PatternFill = new PatternFill { PatternType = PatternValues.Gray125 } }); // required, reserved by Excel
                stylesPart.Stylesheet.Fills.Count = 2;

                stylesPart.Stylesheet.Borders = new Borders();
                stylesPart.Stylesheet.Borders.AppendChild(new Border());
                stylesPart.Stylesheet.Borders.Count = 1;

                stylesPart.Stylesheet.CellStyleFormats = new CellStyleFormats();
                stylesPart.Stylesheet.CellStyleFormats.AppendChild(new CellFormat());
                stylesPart.Stylesheet.CellStyleFormats.Count = 1;
            }

            if (null == stylesPart.Stylesheet.CellFormats)
            {
                stylesPart.Stylesheet.CellFormats = new CellFormats();
                stylesPart.Stylesheet.CellFormats.AppendChild(new CellFormat() { NumberFormatId = 0, FormatId = 0, FontId = 0, BorderId = 0, FillId = 0 });
                stylesPart.Stylesheet.CellFormats.Count = 1;
            }

            var dateFormat = stylesPart.Stylesheet.CellFormats.Elements<CellFormat>().FirstOrDefault(cf => null != cf.ApplyNumberFormat && cf.ApplyNumberFormat && null != cf.NumberFormatId && 14 == cf.NumberFormatId);

            if (null == dateFormat)
            {

                dateFormat = new CellFormat()
                {
                    NumberFormatId = 14, //datetime
                    ApplyNumberFormat = true,
                    FontId = 0,
                    BorderId = 0,
                    FormatId = 0,
                    FillId = 0,
                    ApplyFill = true
                };

                stylesPart.Stylesheet.CellFormats.AppendChild(dateFormat);
                stylesPart.Stylesheet.CellFormats.Count = 1 + stylesPart.Stylesheet.CellFormats.Count;
            }

            uint loop = 0;
            foreach (var format in stylesPart.Stylesheet.CellFormats)
            {
                if (format.Equals(dateFormat))
                {
                    break;
                }
                ++loop;
            }

            this.CellFormatIndices[TypeCode.DateTime] = loop;
        }

        public void FastWriteRow(CellReference startingCell, object[] rowData, bool applyDefaultDataStyle = true)
        {

            Row row = this.SheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex == startingCell.RowIndex);
            if (null == row)
            {
                row = new Row() { RowIndex = startingCell.RowIndex };
                this.SheetData.AppendChild(row);
                this.FastWriteRow(startingCell, rowData, applyDefaultDataStyle);
            }
            else
            {
                var rowCells = row.Elements<Cell>().ToArray();

                if (rowCells.Any())
                {
                    CellReference cellReference = startingCell;
                    foreach (var dataItem in rowData)
                    {
                        this.Write(cellReference, dataItem, applyDefaultDataStyle);
                        cellReference = cellReference.NextCell();
                    }
                }
                else
                {
                    CellReference cellReference = startingCell;
                    foreach (var dataItem in rowData)
                    {
                        Cell cell = new Cell() { CellReference = cellReference };
                        row.AppendChild(cell);
                        this.PopulateCellValue(cell, dataItem, applyDefaultDataStyle);
                        cellReference = cellReference.NextCell();
                    }
                }
            }
        }

        public void Write(CellReference cellReference, object value, bool applyDefaultDataStyle = true)
        {
            Row row = this.SheetData.Elements<Row>().Where(r => r.RowIndex == cellReference.RowIndex).FirstOrDefault();

            if (null == row)
            {
                Row referenceRow = null;
                foreach (var existingRow in this.SheetData.Elements<Row>())
                {
                    if (existingRow.RowIndex > cellReference.RowIndex)
                    {
                        referenceRow = existingRow;
                        break;
                    }
                }
                row = new Row() { RowIndex = cellReference.RowIndex };
                this.SheetData.InsertBefore(row, referenceRow);
            }

            Cell cell = row.Elements<Cell>().Where(c => c.CellReference == cellReference).FirstOrDefault();

            if (null == cell)
            {
                Cell referenceCell = null;
                foreach (Cell existingCell in row.Elements<Cell>())
                {
                    if (0 < string.Compare(existingCell.CellReference.Value, cellReference, true))
                    {
                        referenceCell = existingCell;
                        break;
                    }
                }

                cell = new Cell() { CellReference = cellReference };
                row.InsertBefore(cell, referenceCell);
            }

            try
            {
                this.PopulateCellValue(cell, value, applyDefaultDataStyle);
            }
            catch (Exception populateError)
            {
                throw new Exception($"Error populating cell '{cellReference}' with value '{Convert.ToString(value)}'.", populateError);
            }
        }

        private void RemoveFormula(Cell cell)
        {
            if (null == cell?.CellFormula) return;

            var calculationChain = this.WorkbookPart.CalculationChainPart?.CalculationChain;

            if (null == calculationChain) return;

            var calculationCell = calculationChain.Elements<CalculationCell>().Where(c => c.CellReference == cell.CellReference).FirstOrDefault();

            calculationCell.Remove();
            cell.CellFormula.Remove();
        }

        private void PopulateCellValue(Cell cell, object value, bool applyDefaultDataStyle)
        {
            if (null != value)
            {
                TypeCode typeCode = Type.GetTypeCode(value.GetType());

                switch (typeCode)
                {
                    case TypeCode.DateTime:
                        cell.CellValue = new CellValue(((DateTime)value).ToOADate().ToString(CultureInfo.InvariantCulture));
                        cell.DataType = CellValues.Number;
                        break;
                    case TypeCode.Decimal:
                    case TypeCode.Double:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.SByte:
                    case TypeCode.Single:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        cell.CellValue = new CellValue(Convert.ToString(value, CultureInfo.InvariantCulture));
                        cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                        break;
                    default:
                        cell.CellValue = new CellValue(Convert.ToString(value));
                        cell.DataType = new EnumValue<CellValues>(CellValues.String);
                        break;
                }

                this.RemoveFormula(cell);

                if (applyDefaultDataStyle && this.CellFormatIndices.ContainsKey(typeCode))
                {
                    cell.StyleIndex = this.CellFormatIndices[typeCode];
                }
                return;
            }

            cell.CellValue = new CellValue(Convert.ToString(value));
            cell.DataType = new EnumValue<CellValues>(CellValues.String);
        }

        private Cell GetCell(CellReference cellReference)
        {
            if (CellReference.Empty == cellReference) return null;

            Row row = this.SheetData.Elements<Row>().Where(r => r.RowIndex == cellReference.RowIndex).FirstOrDefault();

            if (null == row) return null;

            Cell cell = row.Elements<Cell>().FirstOrDefault(c => c.CellReference == cellReference);

            return cell;
        }

        public bool TryRead(CellReference cellReference, out object value)
        {
            value = null;

            Cell cell = this.GetCell(cellReference);

            if (null == cell) return false;

            if (CellValues.SharedString == cell.DataType)
            {
                if (null == this.WorkbookPart.SharedStringTablePart?.SharedStringTable || !int.TryParse(cell.CellValue.Text, out int stringIndex))
                {
                    value = null;
                    return false;
                }

                value = this.WorkbookPart.SharedStringTablePart.SharedStringTable.ElementAt(stringIndex).InnerText;
            }
            else
            {
                value = cell.CellValue.Text;
            }

            return true;
        }

        private bool TryGetExistingTable(CellReference startCell, CellReference endCell, out TableDefinitionPart existingTableDefinitionPart)
        {
            CellRange range = new CellRange(startCell, endCell);

            existingTableDefinitionPart = null;

            foreach (var tableDefinitionPart in this.WorksheetPart.TableDefinitionParts)
            {
                if (null != tableDefinitionPart.Table)
                {
                    CellRange existingRange = (CellRange)tableDefinitionPart.Table.Reference.Value;

                    if (range.TryGetIntersection(existingRange, out CellRange intersectionRange))
                    {
                        existingTableDefinitionPart = tableDefinitionPart;
                        break;
                    }
                }
            }

            return null != existingTableDefinitionPart;
        }

        public void CreateTable(CellReference startCell, CellReference endCell)
        {
            if (startCell.ColumnIndex > endCell.ColumnIndex)
            {
                this.CreateTable(endCell, startCell);
                return;
            }

            string cellRegion = new CellRange(startCell, endCell);

            if (!this.TryGetExistingTable(startCell, endCell, out TableDefinitionPart tableDefinitionPart))
            {
                tableDefinitionPart = this.WorksheetPart.AddNewPart<TableDefinitionPart>();
                uint tableId = (uint)this.WorkbookPart.WorksheetParts.Sum(x => x.TableDefinitionParts.Count());

                Table table = new Table()
                {
                    Id = tableId,
                    Name = $"table{tableId}",
                    DisplayName = $"table{tableId}",
                    Reference = cellRegion,
                    TotalsRowShown = false
                };

                uint columnCount = CellReference.ColumnDiff(startCell, endCell);
                TableColumns tableColumns = new TableColumns() { Count = columnCount };

                CellReference currentHeaderCell = startCell;
                for (uint loop = 0; loop < columnCount; loop++)
                {
                    this.TryRead(currentHeaderCell, out object header);
                    TableColumn tableColumn = new TableColumn() { Id = 1 + loop, Name = Convert.ToString(header) };
                    tableColumns.Append(tableColumn);
                    currentHeaderCell = currentHeaderCell.NextCell();
                }

                AutoFilter autoFilter = new AutoFilter() { Reference = cellRegion };

                TableStyleInfo tableStyle = new TableStyleInfo()
                {
                    Name = "TableStyleMedium2",
                    ShowFirstColumn = false,
                    ShowLastColumn = false,
                    ShowRowStripes = true
                };

                table.Append(autoFilter);
                table.Append(tableColumns);
                table.Append(tableStyle);

                tableDefinitionPart.Table = table;

                var worksheetTableParts = this.WorksheetPart.Worksheet.Elements<TableParts>().FirstOrDefault();
                if (null == worksheetTableParts)
                {
                    TableParts tableParts = new TableParts() { Count = 0 };
                    this.WorksheetPart.Worksheet.Append(tableParts);
                    worksheetTableParts = tableParts;
                }

                TablePart tablePart = new TablePart() { Id = this.WorksheetPart.GetIdOfPart(tableDefinitionPart) };
                worksheetTableParts.Count = 1 + worksheetTableParts.Count;
                worksheetTableParts.Append(tablePart);
            }
            else
            {
                tableDefinitionPart.Table.Reference = cellRegion;
            }
        }

        public bool TryGetCellStyleIndex(CellReference cellReference, out uint styleIndex)
        {
            styleIndex = 0;

            Cell cell = this.GetCell(cellReference);
            if (null == cell) return false;
            if (null == cell.StyleIndex || !cell.StyleIndex.HasValue) return false;
            styleIndex = cell.StyleIndex.Value;
            return true;
        }

        public void CopyCellStyle(CellReference fromCell, CellReference toCell)
        {
            if (CellReference.Empty == fromCell || CellReference.Empty == toCell) return;

            Cell sourceCell = this.GetCell(fromCell);

            if (null == sourceCell || null == sourceCell.StyleIndex || !sourceCell.StyleIndex.HasValue) return;
            this.ApplyCellStyle(sourceCell.StyleIndex, toCell);
        }

        public void ApplyCellStyle(uint styleIndex, CellReference cell)
        {
            if (CellReference.Empty == cell) return;

            Cell targetCell = this.GetCell(cell);

            if (null == targetCell) return;
            targetCell.StyleIndex = styleIndex;
        }

        public void Save()
        {
            this.WorksheetPart.Worksheet.Save();
        }

        public override string ToString()
        {
            return $"ExcelWorksheet, [Name = {this.Sheet.Name}]";
        }
    }
}
