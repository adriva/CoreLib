using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Razor;
using System.IO;
using Microsoft.Extensions.FileProviders;

namespace Adriva.Extensions.Reports.Mvc
{

    public static class Extensions
    {
        public static IReportingServiceBuilder AddMvcRenderer(this IReportingServiceBuilder builder, Action<MvcRendererOptions> configure)
        {
            MvcRendererOptions rendererOptions = new MvcRendererOptions();
            configure.Invoke(rendererOptions);

            builder.Services.Configure<MvcRendererOptions>(options =>
            {
                options.ViewProbePath = rendererOptions.ViewProbePath;
                options.DataApiUrl = rendererOptions.DataApiUrl;
                options.CommandApiUrl = rendererOptions.CommandApiUrl;
            });

            builder.Services.Configure<RazorViewEngineOptions>(options =>
            {
                options.FileProviders.Add(new PhysicalFileProvider(Path.GetFullPath(rendererOptions.ViewProbePath)));
                options.FileProviders.Add(new ReportingManifestEmbeddedFileProvider(typeof(MvcRendererOptions).Assembly));

                options.ViewLocationFormats.Add($"~/Views/{{0}}{RazorViewEngine.ViewExtension}");
                options.AreaViewLocationFormats.Add($"~/Views/{{0}}{RazorViewEngine.ViewExtension}");
            });

            builder.Services.AddTransient<IReportRenderer, MvcRenderer>();

            builder.Services.AddTransient<IReportRenderer, MvcChartRenderer>();

            return builder;
        }

        public static Web.Controls.ColumnAlignment ToGridColumnAlignment(this ColumnAlignment columnAlignment)
        {
            return (Web.Controls.ColumnAlignment)((int)columnAlignment);
        }
    }
}