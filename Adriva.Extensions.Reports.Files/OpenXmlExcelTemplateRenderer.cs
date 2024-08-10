using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Adriva.Extensions.Documents;
using Microsoft.Extensions.Logging;

namespace Adriva.Extensions.Reports.Excel
{
    public sealed class OpenXmlExcelTemplateRenderer : ExcelRendererBase<ExcelTemplateRendererContext>, IOpenXmlExcelTemplateRenderer
    {
        public static readonly string RendererName = "OpenXmlExcelTemplate";

        public OpenXmlExcelTemplateRenderer(IDocumentManager documentManager, ILogger<OpenXmlExcelTemplateRenderer> logger) : base(OpenXmlExcelTemplateRenderer.RendererName, documentManager, logger)
        {

        }

        public override async Task RenderOutputAsync(ExcelTemplateRendererContext context /*template file path*/, ReportOutput output)
        {
            if (null == context?.TemplateFilePath) throw new ArgumentException("Invalid context or missing template file.");

            if (0 != string.Compare(".xlsx", Path.GetExtension(context.TemplateFilePath), StringComparison.OrdinalIgnoreCase))
                throw new FileFormatException($"Invalid Excel file specified '{context.TemplateFilePath}'. Only xslx extensions are supported.");

            string outputFilePath = Path.Combine(context.WorkingPath, Guid.NewGuid().ToString());

            if (output.Data.Items.Any())
            {
                using (var templatedDocument = this.DocumentManager.Get<IExcelTemplatedDocument>())
                {
                    templatedDocument.Open(context.TemplateFilePath, outputFilePath);
                    outputFilePath = await templatedDocument.PopulateTemplateAsync(context.WorksheetName, context.StartingCell, base.GetDataAsEnumerable(output), context.ShouldGenerateTable);
                }
            }
            else
            {
                context.ShouldGenerateTable = false;

                using (IExcelWorkbook emptyWorkbook = this.DocumentManager.Get<IExcelWorkbook>())
                {
                    emptyWorkbook.Create(outputFilePath);
                    emptyWorkbook.AddWorksheet(context.WorksheetName);
                }
            }

            context.Output = File.OpenRead(outputFilePath);
        }
    }
}
