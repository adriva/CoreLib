using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Adriva.Common.Core;
using Adriva.Web.Core.Optimization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Adriva.Web.Controls
{

    public abstract class ControlTagHelper : TagHelper
    {
        protected const string ParentOutputKey = "ParentControlOutput";

        protected static string GetControlId(TagHelperOutput output, string prefix = "ctrl_")
        {
            string controlId = $"{prefix}{Utilities.GetRandomId(4)}";

            if (output.Attributes.TryGetAttribute("id", out TagHelperAttribute idAttribute))
            {
                controlId = Convert.ToString(idAttribute.Value);
            }
            else
            {
                output.Attributes.Add("id", controlId);
            }

            return controlId;
        }



        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; }

        protected virtual string[] GetOptimizedResources(AssetFileType fileType) => null;

        protected bool TryParseIntArray(string input, out int[] array)
        {
            array = null;
            if (string.IsNullOrWhiteSpace(input)) return false;

            if (input.StartsWith("[", StringComparison.Ordinal) && input.EndsWith("]", StringComparison.Ordinal))
            {
                return this.TryParseIntArray(input.Substring(1, input.Length - 2), out array);
            }
            else
            {
                var numberArray = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (numberArray.All(x => int.TryParse(x, out int _)))
                {
                    array = numberArray.Select(x => int.Parse(x)).ToArray();
                    return true;
                }
            }

            return false;
        }

        protected bool TryParseStringArray(string input, out string[] array)
        {
            array = null;
            if (string.IsNullOrWhiteSpace(input)) return false;

            if (input.StartsWith("[", StringComparison.Ordinal) && input.EndsWith("]", StringComparison.Ordinal))
            {
                return this.TryParseStringArray(input.Substring(1, input.Length - 2), out array);
            }
            else
            {
                array = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                return true;
            }
        }

        protected string GetScriptFunctionCall(string action, int parameterCount = 1, bool hasReturn = false)
        {
            if (string.IsNullOrWhiteSpace(action)) return null;
            parameterCount = Math.Max(0, parameterCount);

            StringBuilder buffer = new StringBuilder();
            int loop = 0;
            while (0 < parameterCount)
            {
                if (0 < loop)
                    buffer.Append(",");
                buffer.Append($"e{loop}");
                --parameterCount;
                ++loop;
            }

            return $"function({buffer.ToString()}){{ {(hasReturn ? "return" : string.Empty)} ({action}).call(this, {buffer.ToString()}); }}";
        }

        protected string GetScriptFunctionCall(string action, params string[] inputParameterNames)
        {
            if (string.IsNullOrWhiteSpace(action)) return null;

            if (null == inputParameterNames || 0 == inputParameterNames.Length) return this.GetScriptFunctionCall(action, 1);


            int loop = 0;
            StringBuilder inputParameterBuffer = new StringBuilder();
            foreach (string parameterName in inputParameterNames)
            {
                if (0 < loop)
                    inputParameterBuffer.Append(",");
                inputParameterBuffer.Append(parameterName);
                ++loop;
            }

            StringBuilder argumentBuffer = new StringBuilder();
            loop = 0;
            foreach (string parameterName in inputParameterNames)
            {
                if (0 < loop)
                    argumentBuffer.Append(",");
                argumentBuffer.Append($"e{loop}");
                ++loop;
            }


            return $"function({argumentBuffer.ToString()}) {{ ({action}).call(this, {inputParameterBuffer.ToString()}); }}";
        }

        protected IHtmlContent GenerateLoaderScript(string initializerScript)
        {
            if (string.IsNullOrWhiteSpace(initializerScript)) return new StringHtmlContent(string.Empty);

            HtmlContentBuilder builder = new HtmlContentBuilder();
            builder.AppendHtmlLine("<script defer>")
                    .AppendHtmlLine
                    ($@"
                        (function(){{
                            var controlLoader = function(){{
                                $(function(){{
                                    {initializerScript}
                                }})
                            }};

                            if ('complete' === document.readyState){{
                                controlLoader();
                            }}
                            else {{
                                window.addEventListener('load', function(){{
                                    controlLoader();
                                }});
                            }}
                        }})();
                    ")
                    .AppendHtmlLine("</script>");
            return builder;
        }

        protected void WriteContentOnce(string id, IHtmlContentBuilder builder, string content)
        {
            if (null == builder) return;
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));

            string key = $"{this.GetType().FullName}_{id}";

            if (this.ViewContext.HttpContext.Items.ContainsKey(id)) return;

            builder.AppendHtml(content);

            this.ViewContext.HttpContext.Items[id] = 1;
        }

        protected void WriteContentOnce(string id, IHtmlContentBuilder builder, IHtmlContent content)
        {
            if (null == builder) return;
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));

            string key = $"{this.GetType().FullName}_{id}";

            if (this.ViewContext.HttpContext.Items.ContainsKey(id)) return;

            builder.AppendHtml(content);

            this.ViewContext.HttpContext.Items[id] = 1;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            HierarchicalTagHelperOutput current;

            if (!context.Items.ContainsKey(ControlTagHelper.ParentOutputKey))
            {
                current = new HierarchicalTagHelperOutput(output, null, this);
            }
            else
            {
                var parent = context.Items[ControlTagHelper.ParentOutputKey] as HierarchicalTagHelperOutput;
                current = new HierarchicalTagHelperOutput(output, parent, this);
            }

            context.Items[ControlTagHelper.ParentOutputKey] = current;

            IOptimizationContext optimizationContext = this.ViewContext.HttpContext.RequestServices.GetService<IOptimizationContext>();


            if (null != optimizationContext)
            {
                var urlHelperFactory = (IUrlHelperFactory)this.ViewContext.HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>();
                var urlHelper = urlHelperFactory.GetUrlHelper(this.ViewContext);

                var javascriptAssets = this.GetOptimizedResources(AssetFileType.Javascript);
                var cssAssets = this.GetOptimizedResources(AssetFileType.Stylesheet);

                if (null != javascriptAssets && 0 < javascriptAssets.Length)
                {
                    Array.ForEach(javascriptAssets, assetName =>
                    {
                        string assetPath = urlHelper.WebControlResource(assetName);
                        optimizationContext.AddScript(assetPath);
                    });
                }

                if (null != cssAssets && 0 < cssAssets.Length)
                {
                    Array.ForEach(cssAssets, assetName =>
                    {
                        string assetPath = urlHelper.WebControlResource(assetName);
                        optimizationContext.AddStylesheet(assetPath);
                    });
                }
            }

            await this.ProcessAsync(context, current);
            //await output.GetChildContentAsync();
        }

        public virtual void Process(TagHelperContext context, HierarchicalTagHelperOutput output)
        {

        }

        public virtual Task ProcessAsync(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            this.Process(context, output);
            return Task.CompletedTask;
        }
    }


}