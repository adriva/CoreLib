namespace Adriva.Extensions.Reports.Mvc
{
    public sealed class MvcColumnOptions
    {
        public int Priority { get; set; }

        public string Template { get; set; }

        public string Formatter { get; set; }

        public string Renderer { get; set; }
    }
}