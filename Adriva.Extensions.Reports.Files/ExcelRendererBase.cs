using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Adriva.Extensions.Documents;
using Microsoft.Extensions.Logging;

namespace Adriva.Extensions.Reports.Excel
{
    public abstract class ExcelRendererBase<T> : ReportRenderer<T> where T : class
    {
        protected IDocumentManager DocumentManager { get; private set; }
        protected ILogger Logger { get; private set; }

        protected ExcelRendererBase(string name, IDocumentManager documentManager, ILogger logger) : base(name)
        {
            this.DocumentManager = documentManager;
            this.Logger = logger;
        }

        protected IEnumerable<object[]> GetDataAsEnumerable(ReportOutput output)
        {
            yield return output.ColumnDefinitons.Select(c => c.Title).Cast<object>().ToArray(); //returns headers as the first row

            foreach (var dataItem in output.Data.Items)
            {
                object[] rowValues = new object[output.ColumnDefinitons.Count];

                for (int loop = 0; loop < output.ColumnDefinitons.Count; loop++)
                {
                    var columnDefinition = output.ColumnDefinitons[loop];
                    var excelColumnOptions = columnDefinition.GetRendererOptions<ExcelColumnOptions>(this.Name);

                    if (string.IsNullOrWhiteSpace(excelColumnOptions?.Template))
                    {
                        rowValues[loop] = dataItem.GetValue(columnDefinition.Field);
                    }
                    else
                    {
                        rowValues[loop] = Helpers.ApplyTemplate(dataItem, excelColumnOptions.Template);
                    }
                }

                yield return rowValues;
            }

            yield break;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="worksheet"></param>
        /// <param name="cellReference"></param>
        /// <param name="output"></param>
        /// <param name="shouldGenerateTable"></param>
        /// <returns>Last written cell's reference</returns>
        protected CellReference PopulateWorksheet(IExcelWorksheet worksheet, CellReference cellReference, ReportOutput output, ExcelRendererContext context, bool applyDefaultDataStyle = true)
        {
            CellReference startingCell = cellReference;
            CellReference lastCell = startingCell;

            foreach (var header in output.ColumnDefinitons.Select(cd => cd.Title))
            {
                worksheet.Write(cellReference, header, applyDefaultDataStyle);
                cellReference = cellReference.NextCell();
            }

            worksheet.Save();

            if (context.UseFastWrite)
            {

                foreach (var dataItem in output.Data.Items)
                {
                    cellReference = cellReference.NextRow(startingCell.ColumnName);
                    object[] rowValues = new object[output.ColumnDefinitons.Count];

                    for (int loop = 0; loop < output.ColumnDefinitons.Count; loop++)
                    {
                        var columnDefinition = output.ColumnDefinitons[loop];
                        var columnRendererOptions = columnDefinition.GetRendererOptions<ExcelColumnOptions>(this.Name);

                        rowValues[loop] = null;

                        if (string.IsNullOrWhiteSpace(columnRendererOptions?.Template))
                        {
                            rowValues[loop] = dataItem.GetValue(columnDefinition.Field);
                        }
                        else
                        {
                            rowValues[loop] = Helpers.ApplyTemplate(dataItem, columnRendererOptions.Template);
                        }
                    }

                    worksheet.FastWriteRow(cellReference, rowValues, applyDefaultDataStyle);

                    for (int loop = 0; loop < output.ColumnDefinitons.Count; loop++)
                    {
                        cellReference = cellReference.NextCell();
                    }
                    lastCell = cellReference;
                }

            }
            else
            {
                foreach (var dataItem in output.Data.Items)
                {
                    cellReference = cellReference.NextRow(startingCell.ColumnName);

                    for (int loop = 0; loop < output.ColumnDefinitons.Count; loop++)
                    {
                        var columnDefinition = output.ColumnDefinitons[loop];
                        var excelColumnOptions = columnDefinition.GetRendererOptions<ExcelColumnOptions>(this.Name);

                        object value = null;
                        if (string.IsNullOrWhiteSpace(excelColumnOptions?.Template))
                        {
                            value = dataItem.GetValue(columnDefinition.Field);
                        }
                        else
                        {
                            value = Helpers.ApplyTemplate(dataItem, excelColumnOptions.Template);
                        }

                        worksheet.Write(cellReference, value, applyDefaultDataStyle);
                        lastCell = cellReference;
                        cellReference = cellReference.NextCell();
                    }
                }
            }

            if (output.Data.Items.Any() && context.ShouldGenerateTable)
            {
                worksheet.CreateTable(startingCell, cellReference.PreviousCell());
            }

            worksheet.Save();

            return lastCell;
        }
    }
}
