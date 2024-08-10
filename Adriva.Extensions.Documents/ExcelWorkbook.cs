using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Logging;

namespace Adriva.Extensions.Documents
{
    internal sealed class ExcelWorkbook : IExcelWorkbook
    {
        private readonly ILogger Logger;
        private readonly Collection<IExcelWorksheet> WorksheetList = new Collection<IExcelWorksheet>();

        private SpreadsheetDocument SpreadsheetDocument;

        public ReadOnlyCollection<IExcelWorksheet> Worksheets => new ReadOnlyCollection<IExcelWorksheet>(this.WorksheetList);

        public ExcelWorkbook(ILogger<ExcelWorkbook> logger)
        {
            this.Logger = logger;
        }

        public void Open(string path, FileAccess fileAccess)
        {
            this.SpreadsheetDocument = SpreadsheetDocument.Open(path, FileAccess.Read == fileAccess ? false : true);
            this.BuildWorksheetCollection();
        }

        public void Open(Stream stream)
        {
            this.SpreadsheetDocument = SpreadsheetDocument.Open(stream, stream.CanSeek && stream.CanWrite);
            this.BuildWorksheetCollection();
        }

        public void Open(string path, string workingFilePath)
        {
            throw new NotSupportedException("This method is not supported for Excel Workbook files.");
        }

        public void Open(string path)
        {
            this.Open(path, FileAccess.Read);
        }

        public void Create(string path)
        {
            this.SpreadsheetDocument = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook);
            this.CreateDefautParts();
            this.BuildWorksheetCollection();
        }

        public void Create(Stream stream)
        {
            this.SpreadsheetDocument = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
            this.CreateDefautParts();
            this.BuildWorksheetCollection();
        }

        private void CreateDefautParts()
        {
            WorkbookPart workbookPart = this.SpreadsheetDocument.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
        }

        private void BuildWorksheetCollection()
        {
            if (null == this.SpreadsheetDocument.WorkbookPart?.Workbook) return;

            var worksheetParts = this.SpreadsheetDocument.WorkbookPart.GetPartsOfType<WorksheetPart>();

            var worksheets = worksheetParts
                                .Select(wsp => new ExcelWorksheet(this.SpreadsheetDocument.WorkbookPart, wsp))
                                .Cast<IExcelWorksheet>();

            this.WorksheetList.Clear();

            foreach (var worksheet in worksheets)
            {
                this.WorksheetList.Add(worksheet);
            }
        }

        public void Save()
        {
            this.SpreadsheetDocument.WorkbookPart?.Workbook?.Save();
            this.SpreadsheetDocument.Save();
        }

        public IExcelWorksheet AddWorksheet(string name = null)
        {
            if (string.IsNullOrWhiteSpace(name)) name = $"Sheet {this.Worksheets.Count}";

            var worksheetPart = this.SpreadsheetDocument.WorkbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());
            var sheets = this.SpreadsheetDocument.WorkbookPart.Workbook.GetFirstChild<Sheets>();

            if (null == sheets)
            {
                this.SpreadsheetDocument.WorkbookPart.Workbook.AppendChild(new Sheets());
                sheets = this.SpreadsheetDocument.WorkbookPart.Workbook.GetFirstChild<Sheets>();
            }

            Sheet sheet = new Sheet()
            {
                Name = name,
                SheetId = 1 + (uint)sheets.ChildElements.Count,
                Id = this.SpreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart)
            };

            sheets.Append(sheet);

            var worksheet = new ExcelWorksheet(this.SpreadsheetDocument.WorkbookPart, worksheetPart);
            this.WorksheetList.Add(worksheet);
            return worksheet;
        }

        public void Dispose()
        {
            this.SpreadsheetDocument.WorkbookPart.Workbook.Save();
            this.SpreadsheetDocument.Close();
            this.SpreadsheetDocument.Dispose();
        }
    }
}
