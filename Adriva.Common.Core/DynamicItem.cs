using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;

namespace Adriva.Common.Core
{
    [Serializable]
    [JsonArray]
    [JsonConverter(typeof(DynamicItem.DynamicItemConverter))]
    public class DynamicItem : DynamicObject, IEnumerable<KeyValuePair<string, object>>, ICloneable
    {

        internal sealed class DynamicItemConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return typeof(DynamicItem).IsAssignableFrom(objectType);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {

                if (JsonToken.Null == reader.TokenType) return new DynamicItem();

                var jo = JObject.Load(reader);

                DynamicItem dynamicItem = new DynamicItem();

                var enumerator = jo.Properties().GetEnumerator();

                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    object value = null;

                    if (JTokenType.Integer == current.Value.Type)
                    {
                        value = current.Value.Value<int>();
                    }
                    else if (JTokenType.Float == current.Value.Type)
                    {
                        value = current.Value.Value<float>();
                    }
                    else if (JTokenType.Guid == current.Value.Type)
                    {
                        value = current.Value.Value<Guid>();
                    }
                    else if (JTokenType.String == current.Value.Type)
                    {
                        value = current.Value.Value<string>();
                    }
                    else if (JTokenType.Boolean == current.Value.Type)
                    {
                        value = current.Value.Value<bool>();
                    }
                    else
                    {
                        value = current.Value.Value<object>();
                    }

                    dynamicItem.Data.Add(DynamicItem.NormalizeKey(current.Name), value);
                }

                return dynamicItem;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                DynamicItem dynamicItem = value as DynamicItem;

                if (null == dynamicItem) throw new NotSupportedException("Can only convert DynamicItem");

                writer.WriteStartObject();

                var enumerator = dynamicItem.Data.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    var pair = enumerator.Current;
                    writer.WritePropertyName(pair.Key);
                    writer.WriteValue(pair.Value);
                }

                writer.WriteEndObject();
            }
        }

        private readonly Dictionary<string, object> Data = new Dictionary<string, object>();

        private static string NormalizeKey(string key)
        {
            if (null == key) return null;
            return key.ToUpperInvariant();
        }

        public int Count
        {
            get { return this.Data.Count; }
        }

        public object this[string key]
        {
            get { return this.Data[DynamicItem.NormalizeKey(key)]; }
            set { this.Data[DynamicItem.NormalizeKey(key)] = value; }
        }

        [JsonConstructor]
        public DynamicItem() { }

        public DynamicItem(IDictionary<string, object> items)
        {
            if (null == items) return;

            foreach (var item in items)
            {
                string normalizedKey = DynamicItem.NormalizeKey(item.Key);
                this.Data.Add(normalizedKey, item.Value);
            }
        }
        public bool ContainsKey(string key)
        {
            if (null == key) return false;
            key = DynamicItem.NormalizeKey(key);
            return this.Data.ContainsKey(key);
        }

        public T GetValue<T>(string key, T defaultValue)
        {
            if (!this.ContainsKey(key))
            {
                return defaultValue;
            }

            return (T)this[key];
        }

        public void Clear()
        {
            this.Data.Clear();
        }

        #region IEnumerable Implementation

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return this.Data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Data.GetEnumerator();
        }

        #endregion

        #region Dynamic Object Implementation

        public dynamic Dynamic
        {
            get
            {
                return (dynamic)this;
            }
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return this.Data.Keys;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var normalizedKey = DynamicItem.NormalizeKey(binder.Name);
            result = null;

            if (this.Data.ContainsKey(normalizedKey))
            {
                result = this.Data[normalizedKey];
                return true;
            }
            return false;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var normalizedKey = DynamicItem.NormalizeKey(binder.Name);

            this.Data[normalizedKey] = value;

            return true;
        }

        #endregion

        #region ICloneable Implementation

        public object Clone()
        {
            DynamicItem clone = new DynamicItem();

            foreach (var dataItem in this.Data)
            {
                ICloneable clonedItemValue = dataItem.Value as ICloneable;
                if (null != clonedItemValue) clone.Data.Add(dataItem.Key, clonedItemValue);
                else clone.Data.Add(dataItem.Key, dataItem.Value);
            }

            return clone;
        }

        #endregion

    }
}
