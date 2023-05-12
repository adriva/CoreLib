using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Adriva.Web.Controls
{
    public enum ColumnAlignment
    {
        Left,
        Center,
        Right
    }

    [HtmlTargetElement("grid-column", ParentTag = "grid")]
    [RestrictChildren("column-template", "column-renderer", "column-event")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn, ItemNullValueHandling = NullValueHandling.Ignore)]
    public class GridColumn : ControlTagHelper
    {
        [HtmlAttributeName("field")]
        [JsonProperty("field")]
        public string Field { get; set; }

        [HtmlAttributeName("title")]
        [JsonProperty("title")]
        public string Title { get; set; }

        [HtmlAttributeName("ishidden")]
        [JsonProperty("hidden", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsHidden { get; set; }

        [HtmlAttributeName("width")]
        [JsonProperty("width", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Width { get; set; }

        [HtmlAttributeName("minWidth")]
        [JsonProperty("minWidth", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int MinWidth { get; set; }

        [HtmlAttributeNotBound]
        [JsonProperty("tmpl", DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        public string Template { get; set; }

        [HtmlAttributeNotBound]
        [JsonProperty("renderer", DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        public RawString Renderer { get; set; }

        [HtmlAttributeName("priority")]
        [JsonProperty("priority", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Priority { get; set; }

        [HtmlAttributeName("alignment")]
        [JsonProperty("align")]
        [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
        public ColumnAlignment Alignment { get; set; }

        [HtmlAttributeName("format")]
        [JsonProperty("format")]
        public string Format { get; set; }

        [HtmlAttributeNotBound]
        [JsonProperty("events", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IDictionary<string, RawString> Events { get; private set; } = new Dictionary<string, RawString>();

        public override async Task ProcessAsync(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            output.SuppressOutput();
            var str = await output.GetChildContentAsync();

            if (null == output.Parent?.Control) return;
            if (!(output.Parent.Control is Grid grid)) return;
            grid.Columns.Add(this);
        }

        public bool ShouldSerializeEvents()
        {
            return null != this.Events && 0 < this.Events.Count;
        }

        public bool ShouldSerializeWidth()
        {
            return 0 < this.Width;
        }
    }
}