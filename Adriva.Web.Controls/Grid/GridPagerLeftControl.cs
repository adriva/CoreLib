using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Adriva.Web.Controls
{
    [HtmlTargetElement("pager-leftpane-control", ParentTag = "grid-pager")]
    public class GridPagerLeftControl : ControlTagHelper
    {
        public override async Task ProcessAsync(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            output.SuppressOutput();
            var childContent = await output.GetChildContentAsync();
            var innerContent = childContent.GetContent();

            if (output.Parent?.Control is GridPager gridPager)
            {
                gridPager.LeftControls.Add(innerContent);
            }
        }
    }
}