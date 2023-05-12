using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Text;
using Adriva.Common.Core;
using System;
using System.Reflection;

namespace Adriva.Extensions.Reports.Mvc
{

    public class MvcRenderer : ReportRenderer<MvcRendererContext>, IMvcRenderer
    {
        public static readonly string RendererName = "Mvc";

        private const string ReportViewNameKey = "reportTemplate";
        private const string FilterViewNameKey = "viewName";
        private const string DefaultOutputViewName = "reportoutput";
        private const string DefaultFilterViewName = "filteroutput";

        public MvcRenderer() : base(MvcRenderer.RendererName)
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
            context.ViewName = string.IsNullOrWhiteSpace(context.ViewName) ? MvcRenderer.DefaultOutputViewName : context.ViewName;

            await this.RenderViewAsync(context, output);
        }

        public async Task RenderFiltersAsync(MvcRendererContext context, FilterOutput output)
        {
            context.ViewName = string.IsNullOrWhiteSpace(context.ViewName) ? MvcRenderer.DefaultFilterViewName : context.ViewName;

            var items = output.Select(x =>
            {
                if (x is FilterItem<MvcFilterRendererOptions> mvcFilterItem)
                {
                    return mvcFilterItem;
                }
                return new FilterItem<MvcFilterRendererOptions>(x.Definition, x.Definition.GetRendererOptions<MvcFilterRendererOptions>(this.Name), x.Data);
            });
            var model = new FilterOutput<MvcFilterRendererOptions>(output.ReportDefinition, items);
            await this.RenderViewAsync(context, model);
        }

        public static string GetJsonData(ReportOutput output)
        {

            if (null == output.Data.Items || !output.Data.Items.Any()) return Utilities.SafeSerialize(Array.Empty<object>());

            List<DynamicItem> dynamicItems = new List<DynamicItem>();
            Dictionary<string, MethodInfo> formatterMethodsCache = new Dictionary<string, MethodInfo>();

            foreach (var dataItem in output.Data.Items)
            {
                DynamicItem dynamicItem = new DynamicItem();

                foreach (var columnDefiniton in output.ColumnDefinitons)
                {
                    var columnOptions = columnDefiniton.GetRendererOptions<MvcColumnOptions>(MvcRenderer.RendererName);

                    if (string.IsNullOrWhiteSpace(columnOptions?.Template))
                    {
                        if (string.IsNullOrWhiteSpace(columnOptions?.Formatter))
                        {
                            dynamicItem[columnDefiniton.Field] = string.Format($"{{0:{columnDefiniton.Format}}}", dataItem.GetValue(columnDefiniton.Field));
                        }
                        else
                        {
                            dynamicItem[columnDefiniton.Field] = Helpers.ApplyMethodFormatter(dataItem, columnDefiniton.Field, columnOptions.Formatter, formatterMethodsCache);
                        }
                    }
                    else
                    {
                        dynamicItem[columnDefiniton.Field] = Helpers.ApplyTemplate(dataItem, columnOptions.Template);
                    }
                }

                dynamicItems.Add(dynamicItem);
            }

            formatterMethodsCache.Clear();
            return Utilities.SafeSerialize(dynamicItems);
        }

        public async Task RenderJsonDataAsync(MvcRendererContext context, ReportOutput output)
        {
            string dataJson = MvcRenderer.GetJsonData(output);
            string json = $"{{ \"recordCount\": {output.TotalCount}, \"items\": {dataJson} }}";
            var httpContext = context.ActionContext.HttpContext;
            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = 200;
            await httpContext.Response.WriteAsync(json, Encoding.UTF8);
        }
    }
}