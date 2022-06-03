using System;
using System.Collections.Generic;
using Adriva.Common.Core;
using Adriva.Web.Core.Optimization;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;

namespace Adriva.Web.Controls
{
    [HtmlTargetElement("simple-selector")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class SimpleSelector : ControlTagHelper
    {
        private readonly SelectTagHelper SelectTagHelper;

        [JsonProperty("ajax")]
        private readonly SelectorAjax AjaxSettings;

        [JsonProperty("matcher", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private RawString MatcherField;

        [HtmlAttributeName("placeholder")]
        [JsonProperty("placeholder")]
        public string Placeholder { get; set; }

        [HtmlAttributeName("allowclear")]
        [JsonProperty("allowClear")]
        public bool AllowClear { get; set; }

        [HtmlAttributeName("asp-for")]
        public ModelExpression For
        {
            get => this.SelectTagHelper.For;
            set => this.SelectTagHelper.For = value;
        }

        [HtmlAttributeName("matcher")]
        [JsonIgnore]
        public string Matcher
        {
            get => this.MatcherField;
            set => this.MatcherField = value;
        }

        [HtmlAttributeName("readycallback")]
        [JsonIgnore]
        public string ReadyCallback { get; set; }

        [HtmlAttributeName("resultscallback")]
        [JsonIgnore]
        public string ResultsCallback { get; set; }

        [HtmlAttributeName("delay")]
        [JsonIgnore()]
        public int Delay { get; set; } = 300;

        [HtmlAttributeName("url")]
        [JsonIgnore]
        public string Url { get; set; }

        [HtmlAttributeName("usecache")]
        [JsonIgnore()]
        public bool UseCache { get; set; } = true;

        [HtmlAttributeName("idfield")]
        [JsonIgnore]
        public string IdField { get; set; } = "id";

        [HtmlAttributeName("textfield")]
        [JsonIgnore]
        public string TextField { get; set; } = "text";

        [HtmlAttributeName("ismultiple")]
        [JsonIgnore]
        public bool IsMultiple { get; set; } = true;

        [HtmlAttributeName("queryparameter")]
        [JsonIgnore]
        public string QueryParameter { get; set; } = "query";

        [HtmlAttributeName("selecteditems")]
        [JsonIgnore]
        public IEnumerable<dynamic> SelectedItems { get; set; }

        [HtmlAttributeName("selecteditemvalue")]
        [JsonIgnore]
        public Func<dynamic, string> SelectedItemValue { get; set; } = x => x.ToString();

        [HtmlAttributeName("selecteditemtext")]
        [JsonIgnore]
        public Func<dynamic, string> SelectedItemText { get; set; } = x => x.ToString();

        public SimpleSelector(IHtmlGenerator htmlGenerator)
        {
            this.SelectTagHelper = new SelectTagHelper(htmlGenerator);
            this.AjaxSettings = new SelectorAjax();
        }

        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            this.SelectTagHelper.ViewContext = this.ViewContext;
            this.SelectTagHelper.Init(context);
        }

        protected override string[] GetOptimizedResources(AssetFileType fileType)
        {
            switch (fileType)
            {
                case AssetFileType.Javascript:
                    return new[] { "selector.js" };
                case AssetFileType.Stylesheet:
                    return new[] { "selector.css" };
                default:
                    return null;
            }
        }

        public override void Process(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            //output.Content.SetContent(string.Empty);
            string controlId = ControlTagHelper.GetControlId(output, "simpleselector_");

            output.TagName = "select";
            output.TagMode = TagMode.StartTagAndEndTag;

            if (this.IsMultiple)
            {
                output.Attributes.Add("multiple", "multiple");
            }

            this.SelectTagHelper.Process(context, output);

            if (null != this.SelectedItems)
            {
                foreach (var item in this.SelectedItems)
                {
                    output.Content.AppendHtml($"<option value=\"{this.SelectedItemValue?.Invoke(item)}\" selected>{this.SelectedItemText?.Invoke(item)}</option>");
                }
            }

            this.AjaxSettings.Url = this.Url;
            this.AjaxSettings.DataFormatter = $"function(requestData){{ var term = requestData.term; requestData = {{}}; requestData['{this.QueryParameter}'] = term; return requestData; }}";

            if (string.IsNullOrWhiteSpace(this.ResultsCallback))
            {
                this.AjaxSettings.ResultsProcessor = $"function(data){{ var resultItems = data.map(x => {{ var output = new Object(); output.id = x['{this.IdField}']; output.text = x['{this.TextField}']; return output; }}); return {{ results: resultItems }}; }}";
            }
            else
            {
                this.AjaxSettings.ResultsProcessor = $"function(data){{ return {this.ResultsCallback}(data, '{this.IdField}', '{this.TextField}'); }}";
            }

            string json = Utilities.SafeSerialize(this);
            string initializeScript = $"var selector = $('#{controlId}').select2({json});";

            if (!string.IsNullOrWhiteSpace(this.ReadyCallback))
            {
                initializeScript += $"{this.ReadyCallback}(selector);";
            }

            output.PostElement.AppendHtml(base.GenerateLoaderScript(initializeScript));
        }
    }
}