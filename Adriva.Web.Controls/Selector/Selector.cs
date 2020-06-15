using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Adriva.Common.Core;
using Adriva.Web.Core.Optimization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;

namespace Adriva.Web.Controls
{
    [HtmlTargetElement("selector")]
    [RestrictChildren("selector-ajax", "option")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Selector : ControlTagHelper
    {
        private readonly SelectTagHelper SelectTagHelper;

        [JsonProperty("templateResult", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private RawString ResultFormatterField;

        [JsonProperty("language", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private RawString LanguageField;

        [HtmlAttributeName("placeholder")]
        [JsonProperty("placeholder", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Placeholder { get; set; }

        [HtmlAttributeName("minlength")]
        [JsonProperty("minimumInputLength")]
        public int MinimumInputLength { get; set; } = 2;

        [HtmlAttributeName("data")]
        [JsonProperty("data", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IEnumerable Data { get; set; } = null;

        [HtmlAttributeName("language")]
        [JsonIgnore]
        public string Language { get; set; } = null;

        [HtmlAttributeName("allowclear")]
        [JsonProperty("allowClear")]
        public bool AllowClear { get; set; }

        [HtmlAttributeName("asp-for")]
        public ModelExpression For
        {
            get => this.SelectTagHelper.For;
            set => this.SelectTagHelper.For = value;
        }

        [HtmlAttributeName("selecteditems")]
        [JsonIgnore]
        public IEnumerable<SelectListItem> SelectedItems { get; set; }

        [HtmlAttributeName("readycallback")]
        [JsonIgnore]
        public string ReadyCallback { get; set; }

        [HtmlAttributeName("resultformatter")]
        [JsonIgnore]
        public string ResultFormatter
        {
            get => this.ResultFormatterField;
            set => this.ResultFormatterField = value;
        }

        [JsonProperty("ajax", DefaultValueHandling = DefaultValueHandling.Ignore)]
        internal SelectorAjax AjaxSettings { get; set; }

        public Selector(IHtmlGenerator htmlGenerator)
        {
            this.Language = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
            this.SelectTagHelper = new SelectTagHelper(htmlGenerator);
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

        private async Task LoadLanguageAsync()
        {
            if (string.IsNullOrWhiteSpace(this.Language)) return;
            if (this.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase)) return;

            if (0 == string.Compare(this.Language, "tr-TR", StringComparison.OrdinalIgnoreCase))
            {
                this.Language = "tr";
            }

            ResourceLoader loader = new ResourceLoader();

            try
            {
                this.LanguageField = await loader.LoadAsync($"lang.selector.{this.Language}");
            }
            catch
            {
                this.LanguageField = null;
            }
        }

        public override async Task ProcessAsync(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            //output.Content.SetContent(string.Empty);
            string controlId = ControlTagHelper.GetControlId(output, "selector_");

            output.TagName = "select";
            output.TagMode = TagMode.StartTagAndEndTag;

            this.SelectTagHelper.Process(context, output);

            if (null != this.SelectedItems)
            {
                foreach (var item in this.SelectedItems)
                {
                    output.Content.AppendHtml($"<option value=\"{ item.Value}\" selected>{item.Text}</option>");
                }
            }

            await output.GetChildContentAsync();

            if (!string.IsNullOrWhiteSpace(this.ResultFormatterField))
            {
                this.ResultFormatterField = base.GetScriptFunctionCall(this.ResultFormatterField, 1, true);
            }

            await this.LoadLanguageAsync();

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