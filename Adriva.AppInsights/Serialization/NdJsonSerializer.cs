using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Newtonsoft.Json;

namespace Adriva.AppInsights.Serialization
{
    public static class NdJsonSerializer
    {
        public static IEnumerable<T> Deserialize<T>(Stream stream, JsonSerializerSettings jsonSerializerSettings = null) where T : class
        {
            if (null == stream || !stream.CanRead) yield break;

            JsonSerializer serializer = null == jsonSerializerSettings ? JsonSerializer.CreateDefault() : JsonSerializer.CreateDefault(jsonSerializerSettings);
            serializer.DateParseHandling = DateParseHandling.None;
            StreamReader streamReader = new StreamReader(stream);
            JsonTextReader jsonReader = new JsonTextReader(streamReader)
            {
                SupportMultipleContent = true
            };


            while (true)
            {
                try
                {
                    if (!jsonReader.Read()) break;
                }
                catch
                {
                    break;
                }

                T instance = serializer.Deserialize<T>(jsonReader);

                if (null != instance) yield return instance;
            }
        }
    }
}