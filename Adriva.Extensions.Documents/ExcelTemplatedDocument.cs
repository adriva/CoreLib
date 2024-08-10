using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Adriva.Extensions.Documents
{
    public class ExcelTemplatedDocument : IExcelTemplatedDocument
    {
        private readonly Dictionary<TypeCode, uint> CellFormatIndices = new Dictionary<TypeCode, uint>();
        private readonly Dictionary<uint, uint?> ColumnStyles = new Dictionary<uint, uint?>();

        private string WorkingFilePath;
        private SpreadsheetDocument SpreadsheetDocument;
        private bool IsDisposed;

        public void Create(Stream stream)
        {
            throw new NotSupportedException($"{nameof(ExcelTemplatedDocument)} doesn't support this method. You should use the 'Open' method(s) to start working with a template.");
        }

        public void Open(string path, string workingFilePath)
        {
            this.WorkingFilePath = workingFilePath;
            File.Copy(path, this.WorkingFilePath, true);
            this.SpreadsheetDocument = SpreadsheetDocument.Open(this.WorkingFilePath, true);
        }

        public void Open(string path)
        {
            this.Open(path, Path.GetTempFileName());
        }

        public void Open(Stream stream)
        {
            this.WorkingFilePath = Path.GetTempFileName();
            using (var fileStream = File.Open(this.WorkingFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.CopyTo(fileStream);
            }

            this.SpreadsheetDocument = SpreadsheetDocument.Open(this.WorkingFilePath, true);
        }

        private static void ChangeElement<T>(OpenXmlReader reader, Action action) where T : OpenXmlElement
        {
            Type typeOfT = typeof(T);
            do
            {
                reader.Read();
            }
            while (!reader.IsEndElement || typeOfT != reader.ElementType);

            action?.Invoke();
        }

        private static bool TryExtractElement<T>(OpenXmlReader reader, OpenXmlWriter writer, out T element) where T : OpenXmlElement
        {
            if (typeof(T) == reader.ElementType && reader.IsStartElement)
            {
                element = (T)reader.LoadCurrentElement();
                writer.WriteElement(element);
                return true;
            }

            element = null;
            return false;
        }

        public async Task<string> PopulateTemplateAsync(string worksheetName, CellReference startingCell, IEnumerable<object[]> dataRows, bool shouldGenerateTable = false)
        {
            if (string.IsNullOrWhiteSpace(worksheetName)) throw new ArgumentNullException(nameof(worksheetName));
            if (CellReference.Empty == startingCell) throw new ArgumentException(nameof(startingCell));
            if (null == dataRows) throw new ArgumentNullException(nameof(dataRows));

            var sheets = this.SpreadsheetDocument.WorkbookPart.Workbook.GetFirstChild<Sheets>();
            var sheet = sheets.Elements<Sheet>().FirstOrDefault(s => 0 == string.Compare(worksheetName, s.Name, StringComparison.OrdinalIgnoreCase));

            if (null == sheet) throw new ArgumentException($"Wroskheet '{worksheetName}' could not be found in the workbook.");

            this.GenerateStyleSheet();

            var worksheetPart = (WorksheetPart)this.SpreadsheetDocument.WorkbookPart.GetPartById(sheet.Id.Value);
            CellReference lastCell = startingCell;
            TableParts tablePartsElement = null;

            using (var templateStream = new MemoryStream())
            {
                using (var worksheetStream = worksheetPart.GetStream())
                {
                    await worksheetStream.CopyToAsync(templateStream);
                }

                templateStream.Seek(0, SeekOrigin.Begin);
                using (var reader = OpenXmlReader.Create(templateStream))
                using (var writer = OpenXmlWriter.Create(worksheetPart))
                {
                    while (reader.Read()) //read from the template stream
                    {
                        if (reader.IsStartElement)
                        {
                            if (typeof(SheetData) != reader.ElementType) // we're only interested in replacing the SheetData part
                            {
                                if (!ExcelTemplatedDocument.TryExtractElement<Columns>(reader, writer, out Columns columnsElement))
                                {
                                    if (shouldGenerateTable)
                                    {
                                        if (!ExcelTemplatedDocument.TryExtractElement<TableParts>(reader, writer, out tablePartsElement))
                                        {
                                            writer.WriteStartElement(reader);
                                        }
                                    }
                                }
                                else
                                {
                                    if (null != columnsElement)
                                    {
                                        var columns = columnsElement.ChildElements.OfType<Column>();

                                        foreach (var column in columns)
                                        {
                                            if (null != column.Style && column.Style.HasValue && null != column.Min && null != column.Max && column.Min.HasValue && column.Max.HasValue)
                                            {
                                                for (uint loop = column.Min.Value; loop <= column.Max.Value; loop++)
                                                {
                                                    this.ColumnStyles[loop] = column.Style.Value;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                ExcelTemplatedDocument.ChangeElement<SheetData>(reader, () =>
                                {
                                    CellReference cellReference = startingCell;
                                    writer.WriteStartElement(new SheetData()); //start SheetData

                                    foreach (object[] dataRow in dataRows)
                                    {
                                        writer.WriteStartElement(new Row() { RowIndex = cellReference.RowIndex });
                                        foreach (object dataItem in dataRow)
                                        {

                                            Cell cell = new Cell() { CellReference = cellReference };
                                            this.PopulateCellValue(cell, cellReference, dataItem, true);

                                            writer.WriteStartElement(cell);
                                            writer.WriteElement(cell.CellValue);
                                            writer.WriteEndElement();
                                            lastCell = cellReference;
                                            cellReference = cellReference.NextCell();
                                        }
                                        writer.WriteEndElement();
                                        cellReference = cellReference.NextRow(startingCell.ColumnName);
                                    }
                                    writer.WriteEndElement(); //end SheetData
                                });
                            }
                        }
                        else if (reader.IsEndElement)
                        {
                            writer.WriteEndElement();
                        }

                        string text = reader.GetText();
                        if (!string.IsNullOrEmpty(text))
                        {
                            writer.WriteString(text);
                        }
                    }

                    reader.Close();
                    writer.Close();
                }
            }

            if (shouldGenerateTable)
            {
                this.CreateTable(worksheetPart, tablePartsElement, startingCell, lastCell, dataRows.FirstOrDefault());
            }

            this.SpreadsheetDocument.Close();
            return this.WorkingFilePath;
        }

        private void CreateTable(WorksheetPart worksheetPart, TableParts existingTableParts, CellReference startCell, CellReference endCell, object[] headers)
        {
            if (null == headers || 0 == headers.Length) return;

            if (startCell.ColumnIndex > endCell.ColumnIndex)
            {
                this.CreateTable(worksheetPart, existingTableParts, endCell, startCell, headers);
                return;
            }

            string cellRegion = new CellRange(startCell, endCell);

            if (!this.TryGetExistingTable(worksheetPart, startCell, endCell, out TableDefinitionPart tableDefinitionPart))
            {
                tableDefinitionPart = worksheetPart.AddNewPart<TableDefinitionPart>();
                uint tableId = (uint)this.SpreadsheetDocument.WorkbookPart.WorksheetParts.Sum(x => x.TableDefinitionParts.Count());

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
                for (uint loop = 0; loop < headers.Length; loop++)
                {
                    TableColumn tableColumn = new TableColumn() { Id = 1 + loop, Name = Convert.ToString(headers[loop]) };
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

                if (null == existingTableParts)
                {
                    TableParts tableParts = new TableParts() { Count = 0 };
                    worksheetPart.Worksheet.Append(tableParts);
                    existingTableParts = tableParts;
                }

                TablePart tablePart = new TablePart() { Id = worksheetPart.GetIdOfPart(tableDefinitionPart) };
                existingTableParts.Count = 1 + existingTableParts.Count;
                existingTableParts.Append(tablePart);
            }
            else
            {
                tableDefinitionPart.Table.Reference = cellRegion;
                if (null != tableDefinitionPart.Table.AutoFilter)
                {
                    tableDefinitionPart.Table.AutoFilter.Reference = cellRegion;
                }
            }
        }

        private void PopulateCellValue(Cell cell, CellReference cellReference, object value, bool applyDefaultDataStyle)
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

                if (applyDefaultDataStyle)
                {
                    if (this.ColumnStyles.ContainsKey(cellReference.ColumnIndex))
                    {
                        cell.StyleIndex = this.ColumnStyles[cellReference.ColumnIndex];
                    }
                    else if (this.CellFormatIndices.ContainsKey(typeCode))
                    {
                        cell.StyleIndex = this.CellFormatIndices[typeCode];
                    }
                }

                return;
            }

            cell.CellValue = new CellValue(Convert.ToString(value));
            cell.DataType = new EnumValue<CellValues>(CellValues.String);
        }

        private void GenerateStyleSheet()
        {
            var stylesPart = this.SpreadsheetDocument.WorkbookPart.GetPartsOfType<WorkbookStylesPart>().FirstOrDefault();

            if (null == stylesPart)
            {
                stylesPart = this.SpreadsheetDocument.WorkbookPart.AddNewPart<WorkbookStylesPart>();
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

        private bool TryGetExistingTable(WorksheetPart worksheetPart, CellReference startCell, CellReference endCell, out TableDefinitionPart existingTableDefinitionPart)
        {
            CellRange range = new CellRange(startCell, endCell);

            existingTableDefinitionPart = null;
            if (null == worksheetPart.TableDefinitionParts) return false;

            foreach (var tableDefinitionPart in worksheetPart.TableDefinitionParts)
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

        #region IDisposable Implementation

        protected virtual void Dispose(bool disposing)
        {
            if (!this.IsDisposed)
            {
                if (disposing)
                {
                    this.SpreadsheetDocument?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                this.IsDisposed = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ExcelTemplatedDocument()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}