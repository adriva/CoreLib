using System.Collections.Generic;
using System.Threading.Tasks;
using Adriva.Common.Core;
using Adriva.Web.Core.Optimization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;

namespace Adriva.Web.Controls
{

    [HtmlTargetElement("grid")]
    [RestrictChildren("grid-column", "grid-pager", "grid-grouping")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Grid : ControlTagHelper
    {
        [JsonProperty("addNewAction", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private RawString AddNewActionField;

        [JsonProperty("paramNames", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private GridParameterNames GridParameterNames = new GridParameterNames();

        [HtmlAttributeName("autoload")]
        [JsonProperty("autoLoad", DefaultValueHandling = DefaultValueHandling.Include)]
        public bool AutoLoad { get; set; } = false;

        [HtmlAttributeName("datasource")]
        [JsonProperty("dataSource")]
        public object DataSource { get; set; }

        [HtmlAttributeName("primarykey")]
        [JsonProperty("primaryKey", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string PrimaryKey { get; set; }

        [HtmlAttributeName("responsive")]
        [JsonProperty("responsive", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsResponsive { get; set; }

        [HtmlAttributeName("fixedheader")]
        [JsonProperty("fixedHeader")]
        public bool HasFixedHeader { get; set; }

        [HtmlAttributeName("height")]
        [JsonProperty("height", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Height { get; set; }

        [HtmlAttributeName("header")]
        [JsonProperty("title", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Header { get; set; }

        [HtmlAttributeName("resizeCheckInterval")]
        [JsonProperty("resizeCheckInterval", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int ResizeCheckInterval { get; set; } = 501;

        [HtmlAttributeName("addnewaction")]
        [JsonIgnore]
        public string AddNewAction
        {
            get => this.AddNewActionField;
            set => this.AddNewActionField = value;
        }

        [HtmlAttributeName("editaction")]
        [JsonIgnore]
        public string EditAction { get; set; }

        [HtmlAttributeName("deleteaction")]
        [JsonIgnore]
        public string DeleteAction { get; set; }

        [HtmlAttributeName("showaction")]
        [JsonIgnore]
        public string ShowAction { get; set; }

        [HtmlAttributeName("readycallback")]
        [JsonIgnore]
        public string ReadyCallback { get; set; }

        [HtmlAttributeName("pageIndexParameter")]
        [JsonIgnore]
        public string PageIndexParameter
        {
            get => this.GridParameterNames.PageIndex;
            set => this.GridParameterNames.PageIndex = value;
        }

        [HtmlAttributeName("pageSizeParameter")]
        [JsonIgnore]
        public string PageSizeParameter
        {
            get => this.GridParameterNames.PageSize;
            set => this.GridParameterNames.PageSize = value;
        }

        [JsonProperty("columns")]
        [HtmlAttributeNotBound]
        public List<GridColumn> Columns { get; private set; } = new List<GridColumn>();

        [JsonProperty("showHiddenColumnsAsDetails", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [HtmlAttributeName("showHiddenColumnsAsDetails")]
        public bool ShowHiddenColumnsAsDetails { get; set; }

        [JsonProperty("maxRowLimit", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [HtmlAttributeName("maxRowLimit")]
        public int MaximumRowLimit { get; set; } = 20;

        [JsonProperty("culture", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [HtmlAttributeName("culture")]
        public string Culture { get; set; }

        [JsonProperty("dateFormat", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [HtmlAttributeName("dateFormat")]
        public string DateFormat { get; set; }

        [HtmlAttributeNotBound]
        [JsonProperty("pager", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public GridPager Pager { get; set; }

        [HtmlAttributeNotBound]
        [JsonProperty("detailTemplate")]
        public string DetailTemplate { get; set; } = "<div class=\"col-12\"></div>";

        [HtmlAttributeNotBound]
        [JsonProperty("grouping", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public GridGrouping Grouping { get; set; }

        public Grid()
        {
            var defaultCulture = Thread.CurrentThread.CurrentUICulture ?? Thread.CurrentThread.CurrentCulture;
            this.Culture = defaultCulture.Name;
            this.DateFormat = defaultCulture.DateTimeFormat.ShortDatePattern.ToLowerInvariant();
        }

        public bool ShouldSerializeHeight()
        {
            return 0 < this.Height;
        }

        public bool ShouldSerializeGrouping()
        {
            return null != this.Grouping && !string.IsNullOrWhiteSpace(this.Grouping.GroupByField);
        }

        public bool ShouldSerializeGridParameterNames()
        {
            return null != this.GridParameterNames && (!string.IsNullOrWhiteSpace(this.GridParameterNames.PageIndex) || !string.IsNullOrWhiteSpace(this.GridParameterNames.PageSize));
        }

        public bool ShouldSerializeDetailTemplate()
        {
            return this.ShowHiddenColumnsAsDetails;
        }

        protected override string[] GetOptimizedResources(AssetFileType fileType)
        {
            if (AssetFileType.Javascript == fileType)
            {
                return new string[] { "gijgo.js" };
            }
            else if (AssetFileType.Stylesheet == fileType)
            {
                return new string[] { "gijgo.css" };
            }
            return Array.Empty<string>();
        }

        public override async Task ProcessAsync(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            IOptimizationContext optimizationContext = this.ViewContext.HttpContext.RequestServices.GetService<IOptimizationContext>();

            string controlId = ControlTagHelper.GetControlId(output, "grid_");
            output.TagName = "table";
            output.TagMode = TagMode.StartTagAndEndTag;

            await output.GetChildContentAsync();

            if (!string.IsNullOrWhiteSpace(this.ShowAction))
            {
                GridColumn showColumn = new GridColumn();
                showColumn.Events.Add("click", $"{base.GetScriptFunctionCall(this.ShowAction)}");
                showColumn.Width = 50;
                showColumn.Template = "<span class=\"material-icons gj-cursor-pointer\" style=\"color:#ec1d25\">remove_red_eye</span>";
                this.Columns.Add(showColumn);
            }

            if (!string.IsNullOrWhiteSpace(this.EditAction))
            {
                GridColumn editColumn = new GridColumn();
                editColumn.Events.Add("click", $"{base.GetScriptFunctionCall(this.EditAction)}");
                editColumn.Width = 50;
                editColumn.Template = "<span class=\"material-icons gj-cursor-pointer\" title=\"Edit\">edit</span>";
                this.Columns.Add(editColumn);
            }

            if (!string.IsNullOrWhiteSpace(this.DeleteAction))
            {
                GridColumn deleteColumn = new GridColumn();
                deleteColumn.Events.Add("click", $"{base.GetScriptFunctionCall(this.DeleteAction)}");
                deleteColumn.Width = 50;
                deleteColumn.Template = "<span class=\"material-icons gj-cursor-pointer\" title=\"Delete\">delete</span>";
                this.Columns.Add(deleteColumn);
            }

            if (!string.IsNullOrWhiteSpace(this.AddNewAction))
            {
                this.Pager?.LeftControls.Insert(0, $"$('<a data-role=\"add-new\" href=\"javascript:void(0)\">Yeni Ekle</a>')");
            }

            if (this.DataSource is HtmlString htmlString)
            {
                this.DataSource = new RawString(htmlString.Value);
            }

            string json = Utilities.SafeSerialize(this);

            string initializeScript = $"var grid = $('#{controlId}').grid({json});";

            if (!string.IsNullOrWhiteSpace(this.ReadyCallback))
            {
                initializeScript += $"{this.ReadyCallback}(grid);";
            }

            output.PostElement.AppendHtml(base.GenerateLoaderScript(initializeScript));
        }
    }
}