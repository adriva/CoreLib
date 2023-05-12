using System.Threading.Tasks;

namespace Adriva.Extensions.Reports.Excel
{
    public interface IExcelRenderer : IReportRenderer
    {
        Task RenderOutputAsync(ExcelRendererContext context, ReportOutput output);
    }
}
