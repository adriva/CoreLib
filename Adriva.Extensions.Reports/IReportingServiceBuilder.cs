using Microsoft.Extensions.DependencyInjection;

namespace Adriva.Extensions.Reports
{
    public interface IReportingServiceBuilder
    {
        IServiceCollection Services { get; }
    }
}