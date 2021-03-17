using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Adriva.Web.Controls
{
    [HtmlTargetElement("chart-title", ParentTag = "chart-options")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ChartTitle : ControlTagHelper
    {
        [HtmlAttributeName("isvisible")]
        [JsonProperty("display", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsVisible { get; set; }

        [HtmlAttributeName("position")]
        [JsonProperty("position")]
        [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
        public Position Position { get; set; } = Position.Top;

        [HtmlAttributeName("title")]
        [JsonProperty("text")]
        public string Title { get; set; }

        public override void Process(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            output.SuppressOutput();

            if (!(output.Parent?.Control is ChartOptions options)) return;

            options.Title = this;
        }
    }
}