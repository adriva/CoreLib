namespace Adriva.Extensions.Reports
{
    public sealed class QueryParameter
    {
        public string Name { get; private set; }

        public object Value { get; private set; }

        public QueryParameter(string name, object value)
        {
            this.Name = name;
            this.Value = value;
        }

        public override string ToString()
        {
            return $"{this.Name} = '{this.Value ?? "NULL"}', [QueryParameter]";
        }
    }
}