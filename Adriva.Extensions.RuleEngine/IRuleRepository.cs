using System.Collections.Generic;
using System.Threading.Tasks;

namespace Adriva.Extensions.RuleEngine
{
    public interface IRuleRepository
    {
        /// <summary>
        /// Gets the rule data for a given group.
        /// </summary>
        /// <param name="groupName">The name of the group that the rules will be retrieved for.</param>
        /// <returns>A task that represents the async retrieval operation. The result of the task stores the rules retrieved.</returns>
        Task<IEnumerable<Rule>> GetRulesAsync(string groupName);
    }
}