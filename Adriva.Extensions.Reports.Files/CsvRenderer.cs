using System.Linq;
using System.Threading.Tasks;
using Adriva.Extensions.Documents;

namespace Adriva.Extensions.Reports.Csv
{
    public sealed class CsvRenderer : ReportRenderer<CsvRendererContext>, ICsvRenderer
    {
        public static readonly string RendererName = "Csv";
        private readonly IDocumentManager DocumentManager;

        public CsvRenderer(IDocumentManager documentManager) : base(CsvRenderer.RendererName)
        {
            this.DocumentManager = documentManager;
        }

        public override async Task RenderOutputAsync(CsvRendererContext context, ReportOutput output)
        {
            using (ICsvDocument csvDocument = this.DocumentManager.Get<ICsvDocument>())
            {
                csvDocument.Create(context.Output);

                if (context.HasHeaders)
                {
                    string[] titles = output.ColumnDefinitons.Select(c => c.Title).ToArray();
                    await csvDocument.WriteRowAsync(titles);
                }

                foreach (var dataItem in output.Data.Items)
                {
                    object[] rowData = output.GetDataArray(dataItem, null);
                    await csvDocument.WriteRowAsync(rowData);
                }
            }
        }
    }
}
