using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Adriva.Web.Controls
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    [HtmlTargetElement("axis-scale", ParentTag = "chart-scales")]
    public class AxisScale : ControlTagHelper
    {
        [HtmlAttributeName("id")]
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Id { get; set; }

        [HtmlAttributeName("axis")]
        [JsonIgnore]
        public Axis Axis { get; set; }

        [HtmlAttributeName("type")]
        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
        public AxisScaleType Type { get; set; }

        [HtmlAttributeName("position")]
        [JsonProperty("position")]
        [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
        public Position Position { get; set; }

        [HtmlAttributeName("stacked")]
        [JsonProperty("stacked", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsStacked { get; set; }

        [JsonProperty("ticks", DefaultValueHandling = DefaultValueHandling.Ignore)]
        internal AxisTicks Ticks { get; set; }

        public bool ShouldSerializeId() => !string.IsNullOrWhiteSpace(this.Id);

        public override async Task ProcessAsync(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            output.SuppressOutput();

            if (!(output.Parent?.Control is ChartScale chartScale)) return;

            _ = await output.GetChildContentAsync();

            if (Axis.X == this.Axis) chartScale.XAxes.Add(this);
            else if (Axis.Y == this.Axis) chartScale.YAxes.Add(this);
        }

    }
}