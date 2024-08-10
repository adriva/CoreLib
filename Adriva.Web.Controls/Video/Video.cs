using System.Threading.Tasks;
using Adriva.Common.Core;
using Adriva.Web.Core.Optimization;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;

namespace Adriva.Web.Controls
{
    [HtmlTargetElement("video-player")]
    [RestrictChildren("video-controlbar", "video-source")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Video : ControlTagHelper
    {
        [JsonProperty("preload", DefaultValueHandling = DefaultValueHandling.Include)]
        private string Preload { get; set; } = "auto";

        [JsonProperty("aspectRatio", DefaultValueHandling = DefaultValueHandling.Include)]
        private string AspectRatio { get; set; } = "16:9";

        [HtmlAttributeName("hascontrols")]
        [JsonProperty("controls", DefaultValueHandling = DefaultValueHandling.Include)]
        public bool HasControls { get; set; }

        [HtmlAttributeName("isfluid")]
        [JsonProperty("fluid", DefaultValueHandling = DefaultValueHandling.Include)]
        public bool IsFluid { get; set; }

        [HtmlAttributeName("postersrc")]
        [JsonProperty("poster", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string PosterSource { get; set; }

        [JsonProperty("controlBar", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public VideoControlBar ControlBar { get; internal set; }

        protected override string[] GetOptimizedResources(AssetFileType fileType)
        {
            switch (fileType)
            {
                case AssetFileType.Javascript:
                    return new[] { "video.js" };
                case AssetFileType.Stylesheet:
                    return new[] { "videojs.css" };
                default:
                    return null;
            }
        }
        public override async Task ProcessAsync(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            output.Attributes.Clear();
            output.TagName = "video-js";
            output.TagMode = TagMode.StartTagAndEndTag;

            await output.GetChildContentAsync();

            string controlId = ControlTagHelper.GetControlId(output, "video_");

            string configuration = Utilities.SafeSerialize(this);
            string initializerScript = string.Format("videojs('{0}', {1})", controlId, configuration);

            output.PostElement.AppendHtml(base.GenerateLoaderScript(initializerScript));
        }
    }
}