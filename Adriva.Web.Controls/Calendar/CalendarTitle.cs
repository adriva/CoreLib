using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;

namespace Adriva.Web.Controls
{

    [HtmlTargetElement("calendar-title")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class CalendarTitle : CalendarHeaderItem
    {
        protected override string GetControlName()
        {
            return "title";
        }

    }
}