using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Adriva.Web.Controls
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    [HtmlTargetElement("axis-ticks", ParentTag = "axis-scale")]
    public class AxisTicks : ControlTagHelper
    {
        [HtmlAttributeName("min")]
        [JsonProperty("min", DefaultValueHandling = DefaultValueHandling.Include)]
        public double? Minimum { get; set; }

        [HtmlAttributeName("max")]
        [JsonProperty("max", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public double? Maximum { get; set; }

        public bool ShouldSerializeMinimum() => this.Minimum.HasValue;

        public bool ShouldSerializeMaximum() => this.ShouldSerializeMinimum() && this.Maximum.HasValue && this.Minimum < this.Maximum;

        public override void Process(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            output.SuppressOutput();

            if (!(output.Parent?.Control is AxisScale axisScale)) return;

            axisScale.Ticks = this;
        }
    }
}