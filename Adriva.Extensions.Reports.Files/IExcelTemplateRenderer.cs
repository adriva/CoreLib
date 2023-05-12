using System.Threading.Tasks;

namespace Adriva.Extensions.Reports.Excel
{
    public interface IExcelTemplateRenderer : IReportRenderer
    {
        Task RenderOutputAsync(ExcelTemplateRendererContext context, ReportOutput output);
    }

}
