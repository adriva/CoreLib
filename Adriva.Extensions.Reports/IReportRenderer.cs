using System.Threading.Tasks;

namespace Adriva.Extensions.Reports
{
    public interface IReportRenderer
    {
        string Name { get; }

        Task RenderOutputAsync(object context, ReportOutput output);

    }

}