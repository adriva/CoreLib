using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Adriva.Web.Controls
{
    public class HierarchicalTagHelperOutput
    {
        private readonly TagHelperOutput Output;

        public HierarchicalTagHelperOutput Parent { get; private set; }

        public ControlTagHelper Control { get; private set; }

        public List<HierarchicalTagHelperOutput> Children { get; private set; } = new List<HierarchicalTagHelperOutput>();

        public string TagName
        {
            get => this.Output.TagName;
            set => this.Output.TagName = value;
        }

        public TagMode TagMode
        {
            get => this.Output.TagMode;
            set => this.Output.TagMode = value;
        }

        public TagHelperAttributeList Attributes
        {
            get => this.Output.Attributes;
        }

        public TagHelperContent Content
        {
            get => this.Output.Content;
            set => this.Output.Content = value;
        }

        public TagHelperContent PostElement
        {
            get => this.Output.PostElement;
        }

        public TagHelperContent PreElement
        {
            get => this.Output.PreElement;
        }

        public HierarchicalTagHelperOutput(TagHelperOutput output, HierarchicalTagHelperOutput parent, ControlTagHelper control)
        {
            this.Parent = parent;
            this.Output = output;
            this.Control = control;

            if (null != this.Parent)
            {
                this.Parent.Children.Add(this);
            }
        }

        public void Reinitialize(string tagName, TagMode tagMode) => this.Output.Reinitialize(tagName, tagMode);

        public Task<TagHelperContent> GetChildContentAsync() => this.Output.GetChildContentAsync();

        public Task<TagHelperContent> GetChildContentAsync(bool useCachedResult) => this.Output.GetChildContentAsync(useCachedResult);

        public void SuppressOutput() => this.Output.SuppressOutput();

        public static implicit operator TagHelperOutput(HierarchicalTagHelperOutput hierarchicalTagHelperOutput)
        {
            return hierarchicalTagHelperOutput?.Output;
        }
    }
}