using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Adriva.Web.Controls
{
    [HtmlTargetElement("chart-scales", ParentTag = "chart-options")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ChartScale : ControlTagHelper
    {
        [JsonProperty("xAxes")]
        public IList<AxisScale> XAxes { get; private set; } = new List<AxisScale>();

        [JsonProperty("yAxes")]
        public IList<AxisScale> YAxes { get; private set; } = new List<AxisScale>();

        public bool ShouldSerializeXAxes() => 0 < this.XAxes.Count;

        public bool ShouldSerializeYAxes() => 0 < this.YAxes.Count;

        public override async Task ProcessAsync(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            output.SuppressOutput();

            if (!(output.Parent?.Control is ChartOptions chartOptions)) return;

            _ = await output.GetChildContentAsync();
            if (0 == output.Children.Count) return;

            chartOptions.Scale = this;
        }
    }
}