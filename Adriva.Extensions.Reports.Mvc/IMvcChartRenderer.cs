using System.Threading.Tasks;

namespace Adriva.Extensions.Reports.Mvc
{
    public interface IMvcChartRenderer : IReportRenderer
    {
        Task RenderOutputAsync(MvcRendererContext context, ReportOutput output);
    }
}