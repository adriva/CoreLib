using Adriva.Web.Controls;

namespace Adriva.Extensions.Reports.Mvc
{
    public sealed class MvcChartOutputOptions
    {
        public int Height { get; set; } = 400;

        public ChartType ChartType { get; set; } = ChartType.Line;
    }
}