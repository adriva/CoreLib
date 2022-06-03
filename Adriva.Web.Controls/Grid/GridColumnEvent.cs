using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;

namespace Adriva.Web.Controls
{
    [HtmlTargetElement("column-event", ParentTag = "grid-column")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn, ItemNullValueHandling = NullValueHandling.Ignore)]
    public class GridColumnEvent : ControlTagHelper
    {
        [HtmlAttributeName("name")]
        public string Name { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            output.SuppressOutput();

            if (null == output.Parent?.Control) return;
            if (!(output.Parent.Control is GridColumn gridColumn)) return;

            var content = await output.GetChildContentAsync();
            gridColumn.Events[this.Name] = content.GetContent()?.Trim();
        }
    }
}