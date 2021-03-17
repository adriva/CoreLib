using System;

namespace Adriva.Extensions.Reports
{
    public sealed class ReportColumnDefinition : RendererOptionsProvider
    {
        public string Title { get; set; }

        public string Field { get; set; }

        public string Format { get; set; }

        public int Width { get; set; }

        public int MinWidth { get; set; }

        [Obsolete("This property is going to be moved into the renderer options and removed from column definition in a future release.", true)]
        public string Template { get; set; }

        public ColumnAlignment Alignment { get; set; }

    }
}