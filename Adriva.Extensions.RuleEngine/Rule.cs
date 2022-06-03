using System;

namespace Adriva.Extensions.RuleEngine
{
    [Serializable]
    public sealed class Rule
    {
        public long Id { get; set; }

        public long? ParentId { get; set; }

        public string Group { get; set; }

        public string Name { get; set; }

        public string Tag { get; set; }

        public string Expression { get; set; }

        public long Properties { get; set; }

        public DateTime Timestamp { get; set; }

        public Rule Parent { get; set; }

        public override string ToString()
        {
            return $"Rule, [Id = {this.Id}, Name = '{this.Name}']";
        }
    }
}