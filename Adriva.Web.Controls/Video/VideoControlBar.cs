using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;

namespace Adriva.Web.Controls
{
    [HtmlTargetElement("video-controlbar", ParentTag = "video-player")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class VideoControlBar : ControlTagHelper
    {
        [HtmlAttributeName("allowpip")]
        [JsonProperty("pictureInPictureToggle", DefaultValueHandling = DefaultValueHandling.Include)]
        public bool AllowPictureInPicture { get; set; }

        public override void Process(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            output.SuppressOutput();

            if (output.Parent.Control is Video videoControl)
            {
                videoControl.ControlBar = this;
            }
        }
    }
}