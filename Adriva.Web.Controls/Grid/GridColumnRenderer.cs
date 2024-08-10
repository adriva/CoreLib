using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;

namespace Adriva.Web.Controls
{
    [HtmlTargetElement("column-renderer", ParentTag = "grid-column")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn, ItemNullValueHandling = NullValueHandling.Ignore)]
    public class GridColumnRenderer : ControlTagHelper
    {
        public override async Task ProcessAsync(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            output.SuppressOutput();

            if (null == output.Parent?.Control) return;
            if (!(output.Parent.Control is GridColumn gridColumn)) return;

            var content = await output.GetChildContentAsync();
            gridColumn.Renderer = content.GetContent()?.Trim();
        }
    }
}