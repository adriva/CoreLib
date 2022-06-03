namespace Adriva.Extensions.Reports.Mvc
{
    public sealed class MvcOutputOptions
    {
        public int ControlHeight { get; set; } = 600;

        public bool HasFixedHeaders { get; set; } = true;

        public int[] PageSizes { get; set; }
    }
}