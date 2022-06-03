using System.Threading.Tasks;

namespace Adriva.Extensions.RuleEngine
{
    public interface IRuleEngine
    {
        Task<TagsetCollection<TItem>> RunAsync<TItem>(string groupName, params TItem[] inputItems);

        Task<RuleValidationResult> ValidateItemAsync<TItem>(string groupName, TItem item);
    }
}
