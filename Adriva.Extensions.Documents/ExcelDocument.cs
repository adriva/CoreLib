using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Adriva.Common.Core;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Adriva.Extensions.Documents
{

    internal class ExcelDocument : IDocument<ExcelSheetRow>//, IExcelFormatter
    {
        private static readonly string ExcelColumnsAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private SpreadsheetDocument SpreadsheetDocument;
        private WorkbookPart WorkbookPart;
        private uint LastTableId = 1;
        private uint SheetCount = 0;
        private bool CanSave = false;

        public void Create(Stream stream)
        {
            this.SpreadsheetDocument = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
            this.WorkbookPart = this.SpreadsheetDocument.AddWorkbookPart();
            this.WorkbookPart.Workbook = new Workbook();
            this.CanSave = true;
        }

        public void Open(string path)
        {
            var settings = new OpenSettings()
            {
                AutoSave = false
            };
            this.SpreadsheetDocument = SpreadsheetDocument.Open(path, false, settings);
            this.WorkbookPart = this.SpreadsheetDocument.WorkbookPart;
        }

        public void Open(Stream stream)
        {
            var settings = new OpenSettings()
            {
                AutoSave = false
            };
            this.SpreadsheetDocument = SpreadsheetDocument.Open(stream, false, settings);
            this.WorkbookPart = this.SpreadsheetDocument.WorkbookPart;
        }

        public void AddPart<TData>(Expression<Func<TData, ExcelSheetRow>> partMappingExpression, IEnumerable<TData> data) where TData : class
        {
            if (!this.CanSave)
            {
                throw new InvalidProgramException("Cannot add part when the document is opened in read-only mode.");
            }

            WorksheetPart worksheetPart = this.WorkbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet();

            // if (null != this.ColumnWidths)
            // {
            //     Columns columns = new Columns();
            //     uint loop = 1;
            //     foreach (int columnWidth in this.ColumnWidths)
            //     {
            //         if (0 < columnWidth)
            //         {
            //             double excelWidth = ((columnWidth - 5) / 7 * 100 + 0.5) / 100;
            //             columns.Append(new Column() { Min = loop, Max = loop, Width = excelWidth, CustomWidth = true });
            //         }
            //         ++loop;
            //     }
            //     worksheetPart.Worksheet.InsertAt(columns, 0);
            // }

            SheetData sheetData = new SheetData();
            worksheetPart.Worksheet.Append(sheetData);

            var sheets = this.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());

            Sheet sheet = new Sheet()
            {
                Id = this.SpreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart),
                SheetId = ++this.SheetCount,
                Name = $"Sheet {this.SheetCount}"
            };

            sheets.Append(sheet);

            this.PopulateSheetData(sheetData, partMappingExpression.Compile(), data, out string lastCellReference, out ulong columnCount);

            // if (columnCount == (ulong)this.ColumnNames.LongCount())
            // {
            //     this.ConvertSheetToTable(worksheetPart, lastCellReference);
            // }

            worksheetPart.Worksheet.Save();
        }

        private void ConvertSheetToTable(WorksheetPart worksheetPart, string lastCellReference)
        {
            if (null == worksheetPart || string.IsNullOrWhiteSpace(lastCellReference) || 0 == string.Compare("A1", lastCellReference, StringComparison.OrdinalIgnoreCase))
                return;

            TableDefinitionPart tdp = worksheetPart.AddNewPart<TableDefinitionPart>();
            Table table = new Table() { Id = this.LastTableId, Name = $"table{this.LastTableId}", DisplayName = $"table{this.LastTableId}", Reference = $"A1:{lastCellReference}", TotalsRowShown = false };
            // TableColumns tableColumns = new TableColumns() { Count = (uint)this.ColumnNames.Length };

            // uint loop = 1;
            // foreach (var columnName in this.ColumnNames)
            // {
            //     TableColumn tableColumn = new TableColumn() { Id = loop, Name = columnName };
            //     tableColumns.Append(tableColumn);
            //     ++loop;
            // }


            AutoFilter autoFilter = new AutoFilter() { Reference = $"A1:{lastCellReference}" };
            TableStyleInfo tableStyle = new TableStyleInfo()
            {
                Name = "TableStyleMedium2",
                ShowFirstColumn = false,
                ShowLastColumn = false,
                ShowRowStripes = true
            };

            table.Append(autoFilter);
            //table.Append(tableColumns);
            table.Append(tableStyle);

            tdp.Table = table;
            table.Save();

            TableParts tableParts = new TableParts() { Count = 1 };
            TablePart tablePart = new TablePart() { Id = worksheetPart.GetIdOfPart(tdp) };
            tableParts.Append(tablePart);
            worksheetPart.Worksheet.Append(tableParts);

            ++this.LastTableId;
        }

        private void PopulateSheetData<TData>(SheetData sheetData, Delegate partDataMapper, IEnumerable<TData> data, out string cellReference, out ulong columnCount) where TData : class
        {
            columnCount = 0;
            cellReference = null;

            if (null == partDataMapper || null == data) return;

            this.CanSave = true;

            IEnumerator<TData> dataEnumerator = data.GetEnumerator();
            uint rowIndex = 1;

            while (dataEnumerator.MoveNext())
            {
                ulong columnIndex = 0;
                ExcelSheetRow excelRow = (ExcelSheetRow)partDataMapper.DynamicInvoke(dataEnumerator.Current);
                Row row = new Row() { RowIndex = rowIndex };

                foreach (var item in excelRow.Items)
                {
                    cellReference = Utilities.GetBaseString(columnIndex, ExcelDocument.ExcelColumnsAlphabet, 0) + rowIndex;

                    Cell newCell = new Cell() { CellReference = cellReference };
                    newCell.CellValue = new CellValue(Convert.ToString(item));
                    newCell.DataType = new EnumValue<CellValues>(CellValues.String);
                    row.AppendChild(newCell);
                    ++columnIndex;
                }

                if (0 == columnCount) columnCount = columnIndex;

                sheetData.Append(row);
                ++rowIndex;
            }
        }


        public IEnumerator<DocumentData> GetContentEnumerator(object arguments)
        {
            SharedStringTable sharedStringsTable = this.WorkbookPart.SharedStringTablePart?.SharedStringTable;

            foreach (var worksheetPart in this.WorkbookPart.WorksheetParts)
            {
                string worksheetName = Path.GetFileNameWithoutExtension(worksheetPart.Uri.ToString());
                SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();

                foreach (Row row in sheetData.Elements<Row>())
                {
                    List<string> rowData = new List<string>();
                    foreach (Cell cell in row.Elements<Cell>())
                    {
                        if (null == cell.DataType || !cell.DataType.HasValue)
                        {
                            rowData.Add(cell.CellValue?.Text);
                        }
                        else
                        {
                            switch (cell.DataType.Value)
                            {
                                case CellValues.SharedString:
                                    if (null == sharedStringsTable)
                                    {
                                        rowData.Add(null);
                                    }
                                    else
                                    {
                                        if (int.TryParse(cell.CellValue.Text, out int stringIndex))
                                        {
                                            rowData.Add(sharedStringsTable.ElementAtOrDefault(stringIndex)?.InnerText);
                                        }
                                        else
                                        {
                                            rowData.Add("ERROR");
                                        }
                                    }
                                    break;
                                default:
                                    rowData.Add(cell.CellValue?.Text);
                                    break;
                            }
                        }
                    }

                    var documentData = new DocumentData(worksheetName);
                    documentData.AddData(rowData);
                    yield return documentData;
                }
            }

            yield break;
        }

        public void Close()
        {
            if (this.CanSave)
            {
                this.WorkbookPart.Workbook.Save();
            }
            this.SpreadsheetDocument.Close();
            this.SpreadsheetDocument.Dispose();
        }

    }
}
