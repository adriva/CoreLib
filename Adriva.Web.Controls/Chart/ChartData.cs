using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Adriva.Web.Controls
{
    [HtmlTargetElement("chart-data", ParentTag = "chart")]
    [RestrictChildren("chart-dataset")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ChartData : ControlTagHelper
    {
        [JsonProperty("datasets")]
        internal readonly List<ChartDataset> Datasets = new List<ChartDataset>();

        [JsonProperty("labels")]
        private string[] LabelsArray = Array.Empty<string>();

        [HtmlAttributeName("series-names")]
        public string SeriesNames { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            output.SuppressOutput();

            if (!(output.Parent?.Control is Chart chart)) return;

            await output.GetChildContentAsync();

            base.TryParseStringArray(this.SeriesNames, out this.LabelsArray);

            chart.ChartData = this;
        }
    }
}