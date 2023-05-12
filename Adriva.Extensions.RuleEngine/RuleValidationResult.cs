using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Adriva.Extensions.RuleEngine
{
    [Serializable]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class RuleValidationResult : IEnumerable<Exception>
    {
        [JsonProperty("Errors")]
        private readonly List<Exception> Exceptions = new List<Exception>();

        [JsonProperty("Status")]
        public string Status
        {
            get => 0 == this.Exceptions.Count ? "OK" : "Error";
        }

        public void Add(Exception exception)
        {
            this.Exceptions.Add(exception);
        }

        public IEnumerator<Exception> GetEnumerator() => this.Exceptions.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.Exceptions.GetEnumerator();
    }
}
