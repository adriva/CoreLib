using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Adriva.Web.Controls
{
    [HtmlTargetElement("chart-options", ParentTag = "chart")]
    [RestrictChildren("chart-title", "chart-legend", "chart-scales")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ChartOptions : ControlTagHelper
    {
        [JsonProperty("plugins")]
        internal IDictionary<string, object> Plugins = new Dictionary<string, object>();

        [JsonProperty("title", DefaultValueHandling = DefaultValueHandling.Ignore)]
        internal ChartTitle Title;

        [JsonProperty("legend", DefaultValueHandling = DefaultValueHandling.Ignore)]
        internal ChartLegend Legend;

        [JsonProperty("scales", DefaultValueHandling = DefaultValueHandling.Ignore)]
        internal ChartScale Scale { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            output.SuppressOutput();

            if (!(output.Parent?.Control is Chart chart)) return;

            await output.GetChildContentAsync();
            chart.Options = this;
        }
    }
}