using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;

namespace Adriva.Web.Controls
{

    [HtmlTargetElement("once")]
    public class Once : ControlTagHelper
    {
        public override async Task ProcessAsync(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            if (!output.Attributes.TryGetAttribute("id", out TagHelperAttribute idAttribute)
                || string.IsNullOrWhiteSpace(Convert.ToString(idAttribute.Value)))
            {
                throw new ArgumentNullException("<once> tag requires an explicit id attribute to be set.");
            }

            output.SuppressOutput();

            var key = $"awc_once_{idAttribute.Value}";

            if (!this.ViewContext.HttpContext.Items.TryGetValue(key, out object value))
            {
                var childContent = await output.GetChildContentAsync();
                output.Content.SetHtmlContent(childContent);
                this.ViewContext.HttpContext.Items[key] = (byte)0x1;
            }
        }
    }
}