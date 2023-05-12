using Newtonsoft.Json;

namespace Adriva.Web.Controls
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed class GridParameterNames
    {
        [JsonProperty("page", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string PageIndex { get; set; }

        [JsonProperty("limit", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string PageSize { get; set; }

        public bool ShouldSerializePageIndex()
        {
            return !string.IsNullOrWhiteSpace(this.PageIndex);
        }

        public bool ShouldSerializePageSize()
        {
            return !string.IsNullOrWhiteSpace(this.PageSize);
        }
    }
}