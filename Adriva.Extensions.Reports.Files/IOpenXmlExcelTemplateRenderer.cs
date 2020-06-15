using System.Threading.Tasks;

namespace Adriva.Extensions.Reports.Excel
{
    public interface IOpenXmlExcelTemplateRenderer : IReportRenderer
    {
        Task RenderOutputAsync(ExcelTemplateRendererContext context, ReportOutput output);
    }

}
