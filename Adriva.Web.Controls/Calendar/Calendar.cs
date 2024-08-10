using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Adriva.Common.Core;
using Adriva.Web.Core.Optimization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;

namespace Adriva.Web.Controls
{

    [HtmlTargetElement("calendar")]
    [RestrictChildren("calendar-button", "calendar-title")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Calendar : ControlTagHelper
    {
        [JsonProperty("plugins", DefaultValueHandling = DefaultValueHandling.Include)]
        private string[] PluginList;

        [JsonProperty("eventPositioned", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private RawString ItemClickCallbackFunction;

        [JsonProperty("datesRender", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private RawString DatesRenderCallbackFunction;

        [JsonProperty("dateClick", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private RawString DateClickCallbackFunction;

        [JsonProperty("eventAllow", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private RawString DropCallbackFunction;

        [JsonProperty("header")]
        private readonly Dictionary<string, string> Header = new Dictionary<string, string>();

        [HtmlAttributeName("plugins")]
        [JsonIgnore]
        public string Plugins { get; set; } = "dayGrid";

        [HtmlAttributeName("defaultview")]
        [JsonProperty("defaultView", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string DefaultView { get; set; }

        [HtmlAttributeName("readycallback")]
        [JsonIgnore]
        public string ReadyCallback { get; set; }

        [HtmlAttributeName("itemclickcallback")]
        [JsonIgnore]
        public string ItemClickCallback { get; set; }

        [HtmlAttributeName("dateclickcallback")]
        [JsonIgnore]
        public string DateClickCallback { get; set; }

        [HtmlAttributeName("datesrendercallback")]
        [JsonIgnore]
        public string DateRenderCallback { get; set; }

        [HtmlAttributeName("height")]
        [JsonProperty("height", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Height { get; set; } = 500;

        [HtmlAttributeName("droppable")]
        [JsonProperty("droppable")]
        public bool SupportsDrop { get; set; }

        [HtmlAttributeName("locale")]
        [JsonProperty("locale", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Locale { get; set; }

        [HtmlAttributeName("dropcallback")]
        [JsonIgnore]
        public string DropCallback { get; set; }

        [HtmlAttributeNotBound]
        [JsonProperty("customButtons")]
        public Dictionary<string, CalendarHeaderItem> HeaderItems { get; private set; } = new Dictionary<string, CalendarHeaderItem>();

        public bool ShouldSerializeHeaderItems()
        {
            bool shouldSerialize = null != this.HeaderItems && 0 < this.HeaderItems.Count;

            if (shouldSerialize)
            {
                Queue<string> removeQueue = new Queue<string>();

                foreach (var key in this.HeaderItems.Keys)
                {
                    if (!(this.HeaderItems[key] is CalendarButton))
                    {
                        removeQueue.Enqueue(key);
                    }
                }

                while (0 < removeQueue.Count)
                {
                    string key = removeQueue.Dequeue();
                    this.HeaderItems.Remove(key);
                }
            }

            return shouldSerialize;
        }

        public bool ShouldSerializeCheckDropAction()
        {
            return !string.IsNullOrWhiteSpace(this.DropCallback);
        }

        protected override string[] GetOptimizedResources(AssetFileType fileType)
        {
            switch (fileType)
            {
                case AssetFileType.Javascript:
                    return new[] { "fullcalendar.core.js", "fullcalendar.daygrid.js", "fullcalendar.interaction.js" };
                case AssetFileType.Stylesheet:
                    return new[] { "fullcalendar.core.css", "fullcalendar.daygrid.css", "fullcalendar.bootstrap.css" };
                default:
                    return Array.Empty<string>();
            }
        }
        public override async Task ProcessAsync(TagHelperContext context, HierarchicalTagHelperOutput output)
        {
            string controlId = ControlTagHelper.GetControlId(output, "calendar_");

            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;

            base.TryParseStringArray(this.Plugins, out this.PluginList);

            _ = await output.GetChildContentAsync();

            this.Header["left"] = string.Join(" ", this.HeaderItems.Where(b => CalendarItemPosition.Left == b.Value.Position).Select(b => b.Key));
            this.Header["center"] = string.Join(" ", this.HeaderItems.Where(b => CalendarItemPosition.Center == b.Value.Position).Select(b => b.Key));
            this.Header["right"] = string.Join(" ", this.HeaderItems.Where(b => CalendarItemPosition.Right == b.Value.Position).Select(b => b.Key));


            if (!string.IsNullOrWhiteSpace(this.ItemClickCallback))
            {
                this.ItemClickCallbackFunction = $@"function (e) {{ 
                                                    $(e.el).click(function () {{
                                                        var sourceEvent = e.event;

                                                        if ({this.ItemClickCallback}) {{
                                                            {this.ItemClickCallback}(sourceEvent);
                                                        }}
                                                    }});
                                                }}";
            }

            if (!string.IsNullOrWhiteSpace(this.DateClickCallback))
            {
                this.DateClickCallbackFunction = base.GetScriptFunctionCall(this.DateClickCallback, 1);
            }

            if (!string.IsNullOrWhiteSpace(this.DateRenderCallback))
            {
                this.DatesRenderCallbackFunction = base.GetScriptFunctionCall(this.DateRenderCallback, 1);
            }

            if (!string.IsNullOrWhiteSpace(this.DropCallback))
            {
                this.DropCallbackFunction = base.GetScriptFunctionCall(this.DropCallback, 2, true);
            }

            string json = Utilities.SafeSerialize(this);

            string initializeScript = $@"
                    var calendarObject = {{ }};
                    let calendarElement = document.getElementById('{controlId}');
                    calendarObject.calendar = new FullCalendar.Calendar(calendarElement, {json});
                    calendarObject.calendar.render();
            ";

            if (!string.IsNullOrWhiteSpace(this.ReadyCallback))
            {
                initializeScript += $"({base.GetScriptFunctionCall(this.ReadyCallback)})(calendarObject.calendar);";
            }

            output.PostElement.AppendHtml(base.GenerateLoaderScript(initializeScript));
        }
    }
}