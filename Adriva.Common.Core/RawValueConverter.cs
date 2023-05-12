using System;
using Newtonsoft.Json;

namespace Adriva.Common.Core
{
    public class RawValueConverter : JsonConverter
    {

        private static readonly Type TypeOfString = typeof(string);

        public override bool CanConvert(Type objectType)
        {
            return RawValueConverter.TypeOfString.Equals(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return reader.Value;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (null != value)
            {
                writer.WriteRawValue(Convert.ToString(value));
            }
        }
    }
}
