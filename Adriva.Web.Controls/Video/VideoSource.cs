using System.IO;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;

namespace Adriva.Web.Controls
{
    [HtmlTargetElement("video-source", ParentTag = "video-player")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class VideoSource : ControlTagHelper
    {

        [HtmlAttributeName("src")]
        public string Source { get; set; }

        [HtmlAttributeName("ignoretype")]
        public bool IgnoreType { get; set; }

        public override void Process(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            output.Attributes.Clear();
            output.Attributes.Add("src", this.Source);

            if (!this.IgnoreType)
            {
                string mimeType = string.Empty;

                if (!string.IsNullOrWhiteSpace(this.Source))
                {
                    string extension = Path.GetExtension(this.Source);
                    if (0 < extension.Length)
                    {
                        mimeType = $"video/{extension.Substring(1)}";
                    }
                }

                output.Attributes.Add("type", mimeType);
            }

            output.TagName = "source";
            output.TagMode = TagMode.SelfClosing;
        }
    }
}