using System.Threading.Tasks;

namespace Adriva.Extensions.Reports.Mvc
{
    public interface IMvcRenderer : IReportRenderer
    {
        Task RenderOutputAsync(MvcRendererContext context, ReportOutput output);

        Task RenderFiltersAsync(MvcRendererContext context, FilterOutput output);

        Task RenderJsonDataAsync(MvcRendererContext context, ReportOutput output);
    }
}