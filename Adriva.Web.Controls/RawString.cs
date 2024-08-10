using Newtonsoft.Json;

namespace Adriva.Web.Controls
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    [JsonConverter(typeof(RawStringJsonConverter))]
    public class RawString
    {
        [JsonIgnore]
        public string Value { get; private set; }

        public RawString(string value)
        {
            this.Value = value;
        }

        public static implicit operator RawString(string input)
        {
            return new RawString(input);
        }

        public static implicit operator string(RawString rawString)
        {
            return rawString?.Value;
        }
    }
}