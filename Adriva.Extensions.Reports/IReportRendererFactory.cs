namespace Adriva.Extensions.Reports
{
    public interface IReportRendererFactory
    {
        TRendererInterface GetRenderer<TRendererInterface>() where TRendererInterface : IReportRenderer;
    }

}