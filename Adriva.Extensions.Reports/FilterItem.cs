namespace Adriva.Extensions.Reports
{
    public class FilterItem
    {
        public FilterDefinition Definition { get; private set; }

        public IDataSet Data { get; private set; }

        public FilterItem(FilterDefinition definition, IDataSet data)
        {
            this.Definition = definition;
            this.Data = data;
        }
    }

    public sealed class FilterItem<TOptions> : FilterItem
    {
        public TOptions RendererOptions { get; private set; }

        public FilterItem(FilterDefinition definition, TOptions rendererOptions, IDataSet data) : base(definition, data)
        {
            this.RendererOptions = rendererOptions;
        }
    }
}