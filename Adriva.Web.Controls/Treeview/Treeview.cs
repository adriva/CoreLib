using System.Threading.Tasks;
using Adriva.Common.Core;
using Adriva.Web.Core.Optimization;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Adriva.Web.Controls
{

    [HtmlTargetElement("treeview")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Treeview : ControlTagHelper
    {
        private readonly InputTagHelper InputTagHelper;

#pragma warning disable CS0414
        [JsonProperty("uiLibrary")]
        private string uiLibrary = "bootstrap4";
#pragma warning restore CS0414

        [HtmlAttributeName("primarykey")]
        [JsonProperty("primaryKey", DefaultValueHandling = DefaultValueHandling.Populate)]
        public string PrimaryKey { get; set; } = "id";

        [HtmlAttributeName("textfield")]
        [JsonProperty("textField", DefaultValueHandling = DefaultValueHandling.Populate)]
        public string TextField { get; set; } = "text";

        [HtmlAttributeName("childrenfield")]
        [JsonProperty("childrenField", DefaultValueHandling = DefaultValueHandling.Populate)]
        public string ChildrenField { get; set; } = "children";

        [HtmlAttributeName("cascadeselection")]
        [JsonProperty("cascadeSelection", DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool CascadeSelection { get; set; }

        [HtmlAttributeName("autoload")]
        [JsonProperty("autoLoad", DefaultValueHandling = DefaultValueHandling.Include)]
        public bool AutoLoad { get; set; }

        [HtmlAttributeName("lazyload")]
        [JsonProperty("lazyLoading", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool LazyLoad { get; set; }

        [HtmlAttributeName("datasource")]
        [JsonProperty("dataSource", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object DataSource { get; set; }

        [HtmlAttributeName("selectcallback")]
        [JsonIgnore]
        public string SelectCallback { get; set; }

        [HtmlAttributeName("nodedataboundcallback")]
        [JsonIgnore]
        public string NodeDataBoundCallback { get; set; }

        [HtmlAttributeName("readycallback")]
        [JsonIgnore]
        public string ReadyCallback { get; set; }

        [HtmlAttributeName("asp-for")]
        [JsonIgnore]
        public ModelExpression For
        {
            get => this.InputTagHelper.For;
            set => this.InputTagHelper.For = value;
        }

        [HtmlAttributeName("enable-dragdrop")]
        [JsonProperty("dragAndDrop", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsDragDropEnabled { get; set; }

        public Treeview(IHtmlGenerator htmlGenerator)
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
                    return null;
            }
        }
        public override void Process(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;

            var inputAttributes = new TagHelperAttributeList();
            inputAttributes.Add("type", "hidden");
            TagHelperOutput inputTagOutput = new TagHelperOutput("input", inputAttributes, (useCache, encoder) => Task.FromResult((TagHelperContent)new DefaultTagHelperContent()));
            this.InputTagHelper.Process(context, inputTagOutput);
            output.PostElement.AppendHtml(inputTagOutput);

            string inputControlId = ControlTagHelper.GetControlId(inputTagOutput, string.Empty);
            string controlId = ControlTagHelper.GetControlId(output, "tree_");

            string initializeScript = $"var tree = $('#{controlId}').tree({Utilities.SafeSerialize(this)});";

            if (!string.IsNullOrWhiteSpace(this.NodeDataBoundCallback))
            {
                initializeScript += $"tree.on('nodeDataBound', {base.GetScriptFunctionCall(this.NodeDataBoundCallback, 4)});";
            }

            if (!string.IsNullOrWhiteSpace(this.SelectCallback))
            {
                initializeScript += $"tree.on('select', {base.GetScriptFunctionCall(this.SelectCallback, 4)});";
            }

            initializeScript += $"tree.on('select', function(e, node, id, record){{ $('#{inputControlId}').val(id); }});";


            if (!string.IsNullOrWhiteSpace(this.ReadyCallback))
            {
                initializeScript += $"{this.ReadyCallback}(tree);";
            }

            output.PostElement.AppendHtml(base.GenerateLoaderScript(initializeScript));


        }
    }
}