using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Adriva.Common.Core;
using Microsoft.Extensions.Options;

namespace Adriva.Extensions.RuleEngine
{
    public class JsonRuleRepositoryOptions
    {
        public string FilePath { get; set; }
    }

    public class JsonRuleRepository : IRuleRepository
    {
        private readonly JsonRuleRepositoryOptions Options;

        public JsonRuleRepository(IOptions<JsonRuleRepositoryOptions> optionsAccessor)
        {
            this.Options = optionsAccessor.Value;
        }

        public Task<IEnumerable<Rule>> GetRulesAsync(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) throw new ArgumentNullException(nameof(groupName));

            string json = File.ReadAllText(this.Options.FilePath);
            var rules = Utilities.SafeDeserialize<List<Rule>>(json);
            return Task.FromResult(rules.Where(r => 0 == string.Compare(r.Group, groupName, StringComparison.OrdinalIgnoreCase)));
        }
    }
}