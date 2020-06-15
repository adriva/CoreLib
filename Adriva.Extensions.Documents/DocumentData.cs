using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace Adriva.Extensions.Documents
{
    public sealed class DocumentData
    {
        public string Name { get; private set; }

        public StringValues Data { get; private set; }

        public DocumentData(string name)
        {
            this.Name = name;
            this.Data = StringValues.Empty;
        }

        internal void AddData(string data)
        {
            this.Data = StringValues.Concat(this.Data, data);
        }

        internal void AddData(IEnumerable<string> data)
        {
            this.Data = new StringValues(data.ToArray());
        }
    }
}
