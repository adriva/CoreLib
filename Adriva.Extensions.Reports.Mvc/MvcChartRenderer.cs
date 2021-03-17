using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Adriva.Extensions.Reports.Mvc
{
    public class MvcChartRenderer : ReportRenderer<MvcRendererContext>, IMvcChartRenderer
    {
        private const string DefaultOutputViewName = "reportchartoutput";

        public static readonly string RendererName = "MvcChart";

        public MvcChartRenderer() : base(MvcChartRenderer.RendererName)
        {
        }

        private async Task RenderViewAsync(MvcRendererContext context, object model)
        {
            ITempDataDictionaryFactory tempDataDictionaryFactory = context.ActionContext.HttpContext.RequestServices?.GetRequiredService<ITempDataDictionaryFactory>();
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), context.ActionContext.ModelState) { Model = model };

            ViewResult viewResult = new ViewResult()
            {
                ViewName = context.ViewName,
                ViewData = viewData,
                TempData = tempDataDictionaryFactory?.GetTempData(context.ActionContext.HttpContext)
            };

            await viewResult.ExecuteResultAsync(context.ActionContext);
        }

        public override async Task RenderOutputAsync(MvcRendererContext context, ReportOutput output)
        {
            context.ViewName = string.IsNullOrWhiteSpace(context.ViewName) ? MvcChartRenderer.DefaultOutputViewName : context.ViewName;

            string seriesNamesField = null, xAxisTitleField = null, valueField = null;

            foreach (ReportColumnDefinition column in output.ColumnDefinitons)
            {
                MvcChartColumnOptions chartOptions = column.GetRendererOptions<MvcChartColumnOptions>(MvcChartRenderer.RendererName);
                if (null == chartOptions) continue;

                if (chartOptions.IsSeriesName) seriesNamesField = column.Field;
                else if (chartOptions.IsXAxisTitle) xAxisTitleField = column.Field;
                else if (chartOptions.IsValue) valueField = column.Field;

                if (null != seriesNamesField && null != xAxisTitleField && null != valueField) break;
            }

            if (null == seriesNamesField || null == xAxisTitleField || null == valueField)
            {
                throw new InvalidOperationException($"Series names, x-Axis Title and value source must be set for report chart component and a field can not have more than one flag set.");
            }

            MvcReportChartOutput chartOutput = new MvcReportChartOutput(output.Name, output.Definition, output.Data, output.FilterValues);

            IEnumerable<object> seriesNames = output.Data.Items.Select(row => row.GetValue(seriesNamesField)).Distinct();
            IEnumerable<object> xAxisTitles = output.Data.Items.Select(row => row.GetValue(xAxisTitleField)).Distinct();

            chartOutput.XAxisTitles = xAxisTitles.Select(x => Convert.ToString(x));

            foreach (var seriesName in seriesNames)
            {
                List<decimal> seriesValues = new List<decimal>();

                var seriesDataItems = output.Data.Items.ToArray().Where(row => row.GetValue(seriesNamesField).Equals(seriesName));

                foreach (var xAxisTitle in xAxisTitles)
                {
                    var dataRow = seriesDataItems.FirstOrDefault(row => row.GetValue(xAxisTitleField).Equals(xAxisTitle));

                    if (null == dataRow) seriesValues.Add(0);
                    else seriesValues.Add(Convert.ToDecimal(dataRow.GetValue(valueField)));
                }

                chartOutput.Series.Add(new MvcChartSeries(Convert.ToString(seriesName), seriesValues));
            }

            await this.RenderViewAsync(context, chartOutput);
        }
    }
}