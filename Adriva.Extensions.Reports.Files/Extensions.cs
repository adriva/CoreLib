using Adriva.Extensions.Documents;
using Adriva.Extensions.Reports.Csv;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Adriva.Extensions.Reports.Excel
{
    public static class Extensions
    {
        public static IReportingServiceBuilder AddExcelRenderer(this IReportingServiceBuilder builder)
        {
            builder.Services.TryAddSingleton<IDocumentManager, DocumentManager>();
            builder.Services.AddTransient<IReportRenderer, ExcelRenderer>();
            builder.Services.AddTransient<IReportRenderer, ExcelTemplateRenderer>();
            builder.Services.AddTransient<IReportRenderer, OpenXmlExcelTemplateRenderer>();
            return builder;
        }

        public static IReportingServiceBuilder AddCsvRenderer(this IReportingServiceBuilder builder)
        {
            builder.Services.TryAddSingleton<IDocumentManager, DocumentManager>();
            builder.Services.AddTransient<IReportRenderer, CsvRenderer>();
            return builder;
        }
    }
}
