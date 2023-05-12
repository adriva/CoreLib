using Adriva.Common.Core;
using Adriva.Web.Core.Optimization;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;

namespace Adriva.Web.Controls
{
    [HtmlTargetElement("html-editor")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class HtmlEditor : ControlTagHelper
    {
        private readonly TextAreaTagHelper TextAreaTagHelper;

        [HtmlAttributeName("height")]
        [JsonProperty("height", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Height { get; set; }

        [HtmlAttributeName("minheight")]
        [JsonProperty("minHeight", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int MinimumHeight { get; set; }

        [HtmlAttributeName("maxheight")]
        [JsonProperty("maxHeight", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int MaximumHeight { get; set; }

        [HtmlAttributeName("isfocused")]
        [JsonProperty("focus", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsFocused { get; set; }

        [HtmlAttributeName("codefiltersenabled")]
        [JsonProperty("codeviewFilter", DefaultValueHandling = DefaultValueHandling.Include)]
        public bool AllowCodeFilters { get; set; }

        [HtmlAttributeName("iframefiltersenabled")]
        [JsonProperty("codeviewIframeFilter", DefaultValueHandling = DefaultValueHandling.Include)]
        public bool AllowIFrameFilter { get; set; }

        [HtmlAttributeName("placeholder")]
        [JsonProperty("placeholder", DefaultValueHandling = DefaultValueHandling.Include)]
        public string Placeholder { get; set; }

        [HtmlAttributeName("isinmodal")]
        [JsonProperty("dialogsInBody", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsInModal { get; set; }

        [HtmlAttributeName("maximagesize")]
        [JsonProperty("maximumImageFileSize", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long MaximumImageSize { get; set; }

        [HtmlAttributeName("readycallback")]
        [JsonIgnore]
        public string ReadyCallback { get; set; }

        [HtmlAttributeName("asp-for")]
        public ModelExpression For
        {
            get => this.TextAreaTagHelper?.For;
            set => this.TextAreaTagHelper.For = value;
        }

        public HtmlEditor(IHtmlGenerator htmlGenerator)
        {
            this.TextAreaTagHelper = new TextAreaTagHelper(htmlGenerator);
        }

        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            this.TextAreaTagHelper.ViewContext = this.ViewContext;
            this.TextAreaTagHelper.Init(context);
        }
        protected override string[] GetOptimizedResources(AssetFileType fileType)
        {
            switch (fileType)
            {
                case AssetFileType.Javascript:
                    return new[] { "htmleditor.js" };
                case AssetFileType.Stylesheet:
                    return new[] { "htmleditor.css" };
                default:
                    return null;
            }
        }

        public override void Process(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            string controlId = ControlTagHelper.GetControlId(output, "editor_");

            this.TextAreaTagHelper.Process(context, output);

            string json = Utilities.SafeSerialize(this);
            string initializerScript = string.Format(@"var editor = $('#{0}').summernote({1});", controlId, json);

            if (!string.IsNullOrWhiteSpace(this.ReadyCallback))
            {
                initializerScript += $"{this.ReadyCallback}(editor);";
            }

            output.TagName = "textarea";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.PostElement.AppendHtml(base.GenerateLoaderScript(initializerScript));
        }
    }
}