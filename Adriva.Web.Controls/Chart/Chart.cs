using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Adriva.Common.Core;
using Adriva.Web.Core.Optimization;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Adriva.Web.Controls
{
    [HtmlTargetElement("chart")]
    [RestrictChildren("chart-data", "chart-options")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Chart : ControlTagHelper
    {

        [JsonProperty("data", DefaultValueHandling = DefaultValueHandling.Ignore)]
        internal ChartData ChartData;

        [JsonProperty("options", DefaultValueHandling = DefaultValueHandling.Ignore)]
        internal ChartOptions Options;

        [HtmlAttributeName("type")]
        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
        public ChartType Type { get; set; }

        [HtmlAttributeName("colorscheme")]
        [JsonIgnore]
        public ChartColorScheme ColorScheme { get; set; } = ChartColorScheme.officeStudio6;

        [HtmlAttributeName("height")]
        public int Height { get; set; } = 300;

        [HtmlAttributeName("readycallback")]
        [JsonIgnore]
        public string ReadyCallback { get; set; }

        protected override string[] GetOptimizedResources(AssetFileType fileType)
        {
            switch (fileType)
            {
                case AssetFileType.Javascript:
                    return new[] { "charts.js", "chartjs-plugin-colorschemes.min.js" };
                case AssetFileType.Stylesheet:
                    return null;
                default:
                    return null;
            }
        }
        public override async Task ProcessAsync(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            output.SuppressOutput();

            string controlId = ControlTagHelper.GetControlId(output, "chart_");

            output.TagName = "canvas";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.Add("style", $"width: 100% !important; height: {this.Height}px !important;");

            await output.GetChildContentAsync();

            string schemeName = Convert.ToString(this.ColorScheme);

            if (!string.IsNullOrWhiteSpace(schemeName))
            {
                StringBuilder buffer = new StringBuilder();
                int loop = 0;
                while (loop < schemeName.Length && !char.IsUpper(schemeName[loop]))
                {
                    buffer.Append(schemeName[loop]);
                    loop++;
                }
                buffer.Append(".");
                while (loop < schemeName.Length)
                {
                    buffer.Append(schemeName[loop]);
                    loop++;
                }

                schemeName = buffer.ToString();
                this.Options = this.Options ?? new ChartOptions();
                this.Options.Plugins.Add("colorschemes", new Dictionary<string, string>() { { "scheme", schemeName } });
            }

            if (null != this.ChartData?.Datasets && 0 < this.ChartData.Datasets.Count)
            {
                this.ChartData.Datasets[0].Type = this.Type;
            }

            string json = Utilities.SafeSerialize(this);
            string initializeScript = $"var chart = new Chart($('#{controlId}'), {json});";

            if (!string.IsNullOrWhiteSpace(this.ReadyCallback))
            {
                initializeScript += $"({base.GetScriptFunctionCall(this.ReadyCallback)})(chart);";
            }

            output.PostElement.AppendHtml(base.GenerateLoaderScript(initializeScript));
        }
    }
}