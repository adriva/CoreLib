using System;
using Newtonsoft.Json;

namespace Adriva.Web.Controls
{
    public class RawStringJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.Equals(typeof(RawString));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            RawString rawString = value as RawString;
            writer.WriteRawValue(rawString?.Value);
        }
    }
}