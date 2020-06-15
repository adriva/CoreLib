using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;

namespace Adriva.Web.Controls
{

    [HtmlTargetElement("calendar-button")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class CalendarButton : CalendarHeaderItem
    {

        [HtmlAttributeName("text")]
        [JsonProperty("text")]
        public string Text { get; set; }

        [HtmlAttributeName("name")]
        public string Name { get; set; }

        [HtmlAttributeNotBound]
        [JsonProperty("click")]
        public RawString Click { get; set; }

        protected override string GetControlName() => this.Name;

        public override async Task ProcessAsync(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            var childContent = await output.GetChildContentAsync();
            this.Click = base.GetScriptFunctionCall(childContent.GetContent(), "calendarObject.calendar");

            base.Process(context, output);
        }
    }
}