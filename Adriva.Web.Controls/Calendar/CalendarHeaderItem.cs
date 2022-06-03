using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;

namespace Adriva.Web.Controls
{
    public abstract class CalendarHeaderItem : ControlTagHelper
    {

        [HtmlAttributeName("position")]
        [JsonIgnore]
        public CalendarItemPosition Position { get; set; }

        protected abstract string GetControlName();

        public override void Process(TagHelperContext context, HierarchicalTagHelperOutput output)
        {

            if (!(output.Parent?.Control is Calendar calendar)) return;

            output.SuppressOutput();

            calendar.HeaderItems.Add(this.GetControlName(), this);
        }
    }
}