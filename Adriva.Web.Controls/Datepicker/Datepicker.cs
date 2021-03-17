using System;
using System.Text.Encodings.Web;
using Adriva.Common.Core;
using Adriva.Web.Core.Optimization;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;

namespace Adriva.Web.Controls
{

    [HtmlTargetElement("datepicker")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Datepicker : ControlTagHelper
    {
        private const string DefaultFormat = "dd/mm/yyyy";
        private readonly InputTagHelper InputTagHelper;

        [JsonProperty("minDate", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private RawString MinimumDateField;
        private DateTime? MinimumDateValue;

        [JsonProperty("maxDate", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private RawString MaximumDateField;
        private DateTime? MaximumDateValue;

        [JsonProperty("close")]
        private RawString DateSelectedField;

#pragma warning disable CS0414
        [JsonProperty("uiLibrary")]
        private string UiLibrary = "bootstrap4";

        [JsonProperty("iconsLibrary")]
        private string IconsLibrary = "material";
#pragma warning restore CS0414

        [HtmlAttributeName("locale")]
        [JsonProperty("locale")]
        public string Locale { get; set; } = "tr-TR";

        [HtmlAttributeName("format")]
        [JsonProperty("format", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Format { get; set; } = "dd/mm/yyyy";

        [HtmlAttributeName("allowDelete")]
        [JsonProperty("allowDelete", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool AllowDelete { get; set; }

        [HtmlAttributeName("asp-for")]
        public ModelExpression For
        {
            get => this.InputTagHelper?.For;
            set => this.InputTagHelper.For = value;
        }

        [HtmlAttributeName("readycallback")]
        [JsonIgnore]
        public string ReadyCallback { get; set; }

        [HtmlAttributeName("dateselected")]
        [JsonIgnore]
        public string DateSelected
        {
            get => this.DateSelectedField;
            set => this.DateSelectedField = value;
        }

        [HtmlAttributeName("minimum-date")]
        public DateTime? MinimumDate
        {
            get
            {
                return this.MinimumDateValue;
            }
            set
            {
                if (value.HasValue)
                {
                    this.MinimumDateField = $"new Date({value.Value.Year},{value.Value.Month - 1},{value.Value.Day})";
                }
                else
                {
                    this.MinimumDateField = null;
                }
                this.MinimumDateValue = value;
            }
        }

        [HtmlAttributeName("maximum-date")]
        public DateTime? MaximumDate
        {
            get
            {
                return this.MaximumDateValue;
            }
            set
            {
                if (value.HasValue)
                {
                    this.MaximumDateField = $"new Date({value.Value.Year},{value.Value.Month - 1},{value.Value.Day})";
                }
                else
                {
                    this.MaximumDateField = null;
                }
                this.MaximumDateValue = value;
            }
        }

        public Datepicker(IHtmlGenerator htmlGenerator)
        {
            this.InputTagHelper = new InputTagHelper(htmlGenerator);
        }

        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            this.InputTagHelper.ViewContext = this.ViewContext;
            this.InputTagHelper.Init(context);
        }

        protected override string[] GetOptimizedResources(AssetFileType fileType)
        {
            switch (fileType)
            {
                case AssetFileType.Javascript:
                    return new[] { "gijgo.js" };
                case AssetFileType.Stylesheet:
                    return new[] { "gijgo.css" };
                default:
                    return Array.Empty<string>();
            }
        }
        public override void Process(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            output.TagName = "input";
            output.Attributes.SetAttribute("type", "text");
            ((TagHelperOutput)output).AddClass("datepicker", HtmlEncoder.Default);

            string controlId = ControlTagHelper.GetControlId(output, "datepicker_");

            if (string.IsNullOrWhiteSpace(this.Locale)) this.Locale = "en-us";
            if (string.IsNullOrWhiteSpace(this.Format)) this.Format = "dd/mm/yyyy";

            this.Locale = this.Locale.ToLowerInvariant();

            if (null != this.For)
            {
                this.InputTagHelper.Process(context, output);
            }

            string json = Utilities.SafeSerialize(this);

            string initializeScript = $"var datepicker = $('#{controlId}').datepicker({json});";

            if (!string.IsNullOrWhiteSpace(this.ReadyCallback))
            {
                initializeScript += $"({base.GetScriptFunctionCall(this.ReadyCallback)})(datepicker);";
            }


            output.PostElement.AppendHtml(base.GenerateLoaderScript(initializeScript));
        }
    }
}