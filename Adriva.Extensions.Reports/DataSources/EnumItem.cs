namespace Adriva.Extensions.Reports.DataSources
{
    public sealed class EnumItem
    {
        public string Name { get; private set; }

        public object Value { get; private set; }

        public string Description { get; private set; }

        public EnumItem(string name, object value, string description)
        {
            this.Description = description;
            this.Value = value;
            this.Name = name;
        }
    }
}