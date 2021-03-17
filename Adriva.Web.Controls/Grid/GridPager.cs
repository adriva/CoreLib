using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;

namespace Adriva.Web.Controls
{
    [HtmlTargetElement("grid-pager", ParentTag = "grid")]
    [RestrictChildren("pager-leftpane-control", "pager-rightpane-control")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn, ItemNullValueHandling = NullValueHandling.Ignore)]
    public class GridPager : ControlTagHelper
    {
        [JsonProperty("sizes", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private int[] SizeOutput;

        [HtmlAttributeName("pagesize")]
        [JsonProperty("limit", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int PageSize { get; set; }

        [HtmlAttributeName("sizes")]
        [JsonIgnore]
        public string Sizes { get; set; }

        [HtmlAttributeNotBound]
        [JsonProperty("leftControls", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<RawString> LeftControls { get; private set; } = new List<RawString>();

        [HtmlAttributeNotBound]
        [JsonProperty("rightControls", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<RawString> RightControls { get; private set; } = new List<RawString>();

        public bool ShouldSerializeLeftControls()
        {
            return null != this.LeftControls && 0 < this.LeftControls.Count;
        }

        public bool ShouldSerializeRightControls()
        {
            return null != this.RightControls && 0 < this.RightControls.Count;
        }

        public override async Task ProcessAsync(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            output.SuppressOutput();
            _ = await output.GetChildContentAsync();

            if (null == output.Parent?.Control) return;
            if (!(output.Parent.Control is Grid grid)) return;

            base.TryParseIntArray(this.Sizes, out this.SizeOutput);

            this.PageSize = Math.Max(1, this.PageSize);

            grid.Pager = this;
        }
    }
}