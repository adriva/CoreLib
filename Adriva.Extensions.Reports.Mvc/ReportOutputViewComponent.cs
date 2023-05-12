using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;

namespace Adriva.Extensions.Reports.Mvc
{
    [ViewComponent(Name = "ReportOutput")]
    public sealed class ReportOutputViewComponent : ViewComponent
    {
        private readonly IReportingService ReportingService;

        public ReportOutputViewComponent(IReportingService reportingService)
        {
            this.ReportingService = reportingService;
        }

        public async Task<IViewComponentResult> InvokeAsync(object parameters)
        {
            RouteValueDictionary parameterDictionary = new RouteValueDictionary(parameters);

            if (!parameterDictionary.TryGetValue("Name", out object nameValue))
            {
                throw new ArgumentException("Name property is not set. You must set the name of the report.");
            }

            string name = Convert.ToString(nameValue);
            parameterDictionary.Remove("Name");

            string reportTemplate = "~/views/reportoutput.cshtml";

            if (parameterDictionary.TryGetValue("ReportTemplate", out object reportTemplateValue))
            {
                reportTemplate = Convert.ToString(reportTemplateValue);
                parameterDictionary.Remove("ReportTemplate");
            }

            FilterValues filterValues = new FilterValues();
            foreach (var parameterPair in parameterDictionary)
            {
                filterValues.Add(parameterPair.Key, Convert.ToString(parameterPair.Value));
            }

            var reportOutput = await this.ReportingService.GetReportOutputAsync(name, filterValues);
            return this.View(reportTemplate, reportOutput);
        }
    }
}