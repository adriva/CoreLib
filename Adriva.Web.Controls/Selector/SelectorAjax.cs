using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Adriva.Common.Core;
using Adriva.Web.Core.Optimization;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;

namespace Adriva.Web.Controls
{
    [HtmlTargetElement("selector-ajax", ParentTag = "selector")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]

    public class SelectorAjax : ControlTagHelper
    {
        [JsonProperty("processResults", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private RawString ResultsProcessorField;

        [JsonIgnore]
        private RawString UrlBuilderField;

        [JsonProperty("data", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private RawString DataFormatterField;


        [HtmlAttributeName("url")]
        [JsonProperty("url")]
        public string Url { get; set; }

        [HtmlAttributeName("url")]
        [JsonIgnore]
        public string UrlBuilder
        {
            get => this.UrlBuilderField;
            set => this.UrlBuilderField = value;
        }

        [HtmlAttributeName("requestformatter")]
        [JsonIgnore]
        public string DataFormatter
        {
            get => this.DataFormatterField;
            set => this.DataFormatterField = value;
        }

        [HtmlAttributeName("delay")]
        [JsonProperty("delay")]
        public int Delay { get; set; } = 300;

        [HtmlAttributeName("datatype")]
        [JsonProperty("dataType")]
        public string DataType { get; set; } = "json";

        [HtmlAttributeName("usecache")]
        [JsonProperty("cache", DefaultValueHandling = DefaultValueHandling.Include)]
        public bool UseCache { get; set; } = true;

        [HtmlAttributeName("resultprocessor")]
        public string ResultsProcessor
        {
            get => this.ResultsProcessorField;
            set => this.ResultsProcessorField = value;
        }

        public override void Process(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            output.SuppressOutput();

            if (!(output.Parent.Control is Selector selector)) return;

            if (!string.IsNullOrWhiteSpace(this.UrlBuilderField))
            {
                this.Url = base.GetScriptFunctionCall(this.UrlBuilder, "params");
            }

            if (!string.IsNullOrWhiteSpace(this.DataFormatterField))
            {
                this.DataFormatter = base.GetScriptFunctionCall(this.DataFormatter, 1, true);
            }

            if (!string.IsNullOrWhiteSpace(this.ResultsProcessor))
            {
                this.ResultsProcessor = base.GetScriptFunctionCall(this.ResultsProcessor, 1, true);
            }

            string json = Utilities.SafeSerialize(this);
            selector.AjaxSettings = this;
        }
    }
}