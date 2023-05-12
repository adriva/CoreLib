using System;
using Newtonsoft.Json;

namespace Adriva.Extensions.Notifications
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Recipient
    {
        [JsonProperty("address")]
        public string Address { get; }

        [JsonProperty("fullname")]
        public string FullName { get; }

        [JsonProperty("tag", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object Tag { get; }

        public Recipient(string address, string fullName, object tag)
        {
            this.Address = address;
            this.FullName = fullName;
            this.Tag = tag;
        }
    }
}
