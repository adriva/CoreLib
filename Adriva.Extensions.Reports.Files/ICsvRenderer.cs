using System.Threading.Tasks;

namespace Adriva.Extensions.Reports.Csv
{
    public interface ICsvRenderer
    {
        Task RenderOutputAsync(CsvRendererContext context, ReportOutput output);
    }
}
