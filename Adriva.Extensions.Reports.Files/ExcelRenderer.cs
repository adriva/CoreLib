using System.IO;
using Adriva.Extensions.Documents;
using Microsoft.Extensions.Logging;

namespace Adriva.Extensions.Reports.Excel
{

    public sealed class ExcelRenderer : ExcelRendererBase<ExcelRendererContext>, IExcelRenderer
    {
        public static readonly string RendererName = "Excel";

        public ExcelRenderer(IDocumentManager documentManager, ILogger<ExcelRenderer> logger) : base(ExcelRenderer.RendererName, documentManager, logger)
        {

        }

        public override void RenderOutput(ExcelRendererContext context, ReportOutput output)
        {
            CellReference.TryParse(context.StartingCell, out CellReference cellReference);

            context.Output.Seek(0, SeekOrigin.Begin);

            using (var workbook = this.DocumentManager.Get<IExcelWorkbook>())
            {
                workbook.Create(context.Output);

                var worksheet = workbook.AddWorksheet("Report");
                base.PopulateWorksheet(worksheet, cellReference, output, context);
            }

            context.Output.Seek(0, SeekOrigin.Begin);
        }
    }
}
