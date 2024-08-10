using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Adriva.Extensions.Documents;
using Microsoft.Extensions.Logging;

namespace Adriva.Extensions.Reports.Excel
{
    /// <summary>
    /// Copies the given template XLSX file and tries to populate it
    /// </summary>
    public sealed class ExcelTemplateRenderer : ExcelRendererBase<ExcelTemplateRendererContext>, IExcelTemplateRenderer
    {
        public static readonly string RendererName = "ExcelTemplate";

        public ExcelTemplateRenderer(IDocumentManager documentManager, ILogger<ExcelTemplateRenderer> logger) : base(ExcelTemplateRenderer.RendererName, documentManager, logger)
        {

        }

        public override async Task RenderOutputAsync(ExcelTemplateRendererContext context /*template file path*/, ReportOutput output)
        {
            if (null == context?.TemplateFilePath) throw new ArgumentException("Invalid context or missing template file.");

            if (0 != string.Compare(".xlsx", Path.GetExtension(context.TemplateFilePath), StringComparison.OrdinalIgnoreCase))
                throw new FileFormatException($"Invalid Excel file specified '{context.TemplateFilePath}'. Only xslx extensions are supported.");

            string tempFilePath = null;

            if (string.IsNullOrEmpty(context.WorkingPath))
            {
                tempFilePath = Path.GetTempFileName();
            }
            else
            {
                tempFilePath = Path.Combine(context.WorkingPath, Guid.NewGuid().ToString());
            }

            if (output.Data.Items.Any())
            {
                File.Copy(context.TemplateFilePath, tempFilePath, true);
            }
            else
            {
                context.ShouldGenerateTable = false;

                using (IExcelWorkbook emptyWorkbook = this.DocumentManager.Get<IExcelWorkbook>())
                {
                    emptyWorkbook.Create(tempFilePath);
                    emptyWorkbook.AddWorksheet(context.WorksheetName);
                }
            }

            Stream templateStream = null;
            try
            {
                using (IExcelWorkbook workbook = this.DocumentManager.Get<IExcelWorkbook>())
                {
                    if (ProcessingMode.Default == context.Mode)
                    {
                        templateStream = File.Open(tempFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    }
                    else if (ProcessingMode.Memory == context.Mode)
                    {
                        templateStream = new MemoryStream();
                        byte[] fileBuffer = File.ReadAllBytes(tempFilePath);
                        await templateStream.WriteAsync(fileBuffer, 0, fileBuffer.Length);
                    }

                    workbook.Open(templateStream);

                    IExcelWorksheet worksheet = workbook.Worksheets.FirstOrDefault(ws => 0 == string.Compare(ws.Name, context.WorksheetName, StringComparison.OrdinalIgnoreCase));

                    if (null == worksheet)
                    {
                        throw new ArgumentOutOfRangeException($"Specified worksheet '{context.WorksheetName}' could not be found in the workbook.");
                    }

                    CellReference.TryParse(context.StartingCell, out CellReference cellReference);
                    CellReference lastCell = base.PopulateWorksheet(worksheet, cellReference, output, context, !context.KeepCellStyle);

                    if (lastCell.RowIndex > 1 + cellReference.RowIndex && context.KeepCellStyle)
                    {
                        CellReference dataStartCell = cellReference.NextRow();
                        CellReference currentCell = dataStartCell;

                        // cell col index , style index
                        Dictionary<uint, uint?> cellStyleLookup = new Dictionary<uint, uint?>();

                        while (currentCell.ColumnIndex <= lastCell.ColumnIndex)
                        {
                            if (worksheet.TryGetCellStyleIndex(currentCell, out uint styleIndex))
                            {
                                cellStyleLookup[currentCell.ColumnIndex] = styleIndex;
                            }
                            else
                            {
                                cellStyleLookup[currentCell.ColumnIndex] = null;
                            }

                            currentCell = currentCell.NextCell();
                        }

                        currentCell = currentCell.NextRow(dataStartCell.ColumnName);

                        if (cellStyleLookup.Any(p => p.Value.HasValue))
                        {
                            while (currentCell.RowIndex <= lastCell.RowIndex)
                            {
                                while (currentCell.ColumnIndex <= lastCell.ColumnIndex)
                                {
                                    if (cellStyleLookup[currentCell.ColumnIndex].HasValue)
                                    {
                                        worksheet.ApplyCellStyle(cellStyleLookup[currentCell.ColumnIndex].Value, currentCell);
                                    }
                                    currentCell = currentCell.NextCell();
                                }

                                currentCell = currentCell.NextRow(cellReference.ColumnName);
                            }
                        }
                    }

                }

                await templateStream.FlushAsync();
                templateStream.Seek(0, SeekOrigin.Begin);
                await templateStream.CopyToAsync(context.Output);
            }
            finally
            {
                templateStream?.Dispose();
            }

            context.Output.Seek(0, SeekOrigin.Begin);
        }
    }
}
