using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Adriva.Web.Controls
{
    [HtmlTargetElement("chart-dataset", ParentTag = "chart-data")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ChartDataset : ControlTagHelper
    {
        [HtmlAttributeName("title")]
        [JsonProperty("label")]
        public string Title { get; set; }

        [HtmlAttributeName("data")]
        [JsonProperty("data")]
        public object Data { get; set; }

        /*[HtmlAttributeName("color")]
        [JsonProperty("backgroundColor")]
        public string Color { get; set; } = "rgba(255, 99, 132, 0.2)";

        [HtmlAttributeName("border-color")]
        [JsonProperty("borderColor")]
        public string BorderColor { get; set; } = "rgba(255, 99, 132, 0.2)";*/

        [HtmlAttributeName("border-width")]
        [JsonProperty("borderWidth")]
        public int BorderWidth { get; set; } = 1;

        [HtmlAttributeName("type")]
        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
        public ChartType Type { get; set; }

        public override void Process(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            output.SuppressOutput();

            if (!(output.Parent?.Control is ChartData chartData)) return;

            chartData.Datasets.Add(this);
        }
    }
}