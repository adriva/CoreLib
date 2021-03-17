using Newtonsoft.Json;

namespace Adriva.AppInsights.Serialization.Contracts
{
    public partial class Base
    {
        [JsonProperty("baseType")]
        public string BaseType { get; set; }

        public Base()
            : this("AI.Base", "Base")
        { }

        protected Base(string fullName, string name)
        {
            this.BaseType = string.Empty;
        }
    }
}