using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;

namespace Adriva.Web.Controls
{
    [HtmlTargetElement("grid-grouping", ParentTag = "grid")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn, ItemNullValueHandling = NullValueHandling.Ignore)]
    public class GridGrouping : ControlTagHelper
    {

        [HtmlAttributeName("groupby")]
        [JsonProperty("groupBy", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string GroupByField { get; set; }

        [HtmlAttributeName("title")]
        [JsonProperty("groupByTitle", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string GroupByTitle { get; set; }

        public override void Process(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            output.SuppressOutput();

            if (null == output.Parent?.Control) return;
            if (!(output.Parent.Control is Grid grid)) return;

            grid.Grouping = this;
        }
    }
}