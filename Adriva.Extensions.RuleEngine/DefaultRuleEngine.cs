using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Adriva.Common.Core.DataStructures;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adriva.Extensions.RuleEngine
{
    public sealed class DefaultRuleEngine : IRuleEngine
    {
        // shared across all instances since rule engine is registered as a transient service
        // and it's registered as a scoped service because the repository it depends on maybe a transient service
        private static IMemoryCache Cache;

        private readonly ILogger Logger;
        private readonly RuleEngineOptions Options;
        private readonly IRuleRepository Repository;
        private readonly ScriptOptions DefaultScriptOptions;
        private readonly string RepositoryName;

        public DefaultRuleEngine(IOptions<RuleEngineOptions> optionsAccessor, IRuleRepository repository, ILogger<DefaultRuleEngine> logger)
        {
            this.Logger = logger;
            this.Options = optionsAccessor?.Value ?? new RuleEngineOptions();
            this.Repository = repository;
            this.RepositoryName = this.Repository.GetType().FullName;
            this.DefaultScriptOptions = ScriptOptions.Default.AddReferences(this.Options.Assemblies);

            if (null == DefaultRuleEngine.Cache)
            {
                if (this.Options.UseCache) DefaultRuleEngine.Cache = new MemoryCache(new MemoryCacheOptions());
                else DefaultRuleEngine.Cache = new NullMemoryCache();
            }

            this.Logger.LogInformation($"Using repository '{this.RepositoryName}'");
            this.Logger.LogTrace($"Importing assemblies: {Environment.NewLine}{string.Join(Environment.NewLine, this.Options.Assemblies)}");
        }

        private async Task<IEnumerable<TreeNode<Rule>>> BuildGraphAsync(string groupName)
        {
            return await DefaultRuleEngine.Cache.GetOrCreateAsync($"Group:{this.RepositoryName}:{groupName}", async (entry) =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(10);
                var rules = await this.Repository.GetRulesAsync(groupName);
                return rules.BuildGraph();
            });
        }

        private async Task<bool> RunRuleAsync<TItem>(Rule rule, TItem item)
        {
            try
            {
                var ruleFunction = await DefaultRuleEngine.Cache.GetOrCreateAsync($"Rule:{this.RepositoryName}:{rule.Id}", async (entry) =>
                {
                    entry.SlidingExpiration = TimeSpan.FromMinutes(10);
                    return await CSharpScript.EvaluateAsync<Func<TItem, bool>>(rule.Expression, this.DefaultScriptOptions);
                });

                return ruleFunction.Invoke(item);
            }
            catch (CompilationErrorException compilationError)
            {
                throw new AggregateException(new RuleEngineException($"Error compiling rule '{rule.Id}'."), compilationError);
            }
        }

        private async Task<bool> RunOnNodeAsync<TItem>(TreeNode<Rule> node, TItem item)
        {
            this.Logger.LogTrace($"Running rule '{node.Value.Id}' with Expression '{node.Value.Expression}' on '{item}'.");

            bool selfResult = await this.RunRuleAsync(node.Value, item);

            this.Logger.LogInformation($"Rule '{node.Value.Id}' with Expression '{node.Value.Expression}' {(selfResult ? "Succeeded" : "Failed")} on '{item}'.");

            if (!selfResult) return false;

            foreach (var childNode in node.Children)
            {
                if (await this.RunOnNodeAsync(childNode, item)) return true;
                else return false;
            }

            return true;
        }

        public async Task<RuleValidationResult> ValidateItemAsync<TItem>(string groupName, TItem item)
        {
            RuleValidationResult output = new RuleValidationResult();

            if (string.IsNullOrWhiteSpace(groupName))
            {
                output.Add(new ArgumentNullException(nameof(groupName)));
                return output;
            }

            IEnumerable<TreeNode<Rule>> ruleGraph = null;

            try
            {
                ruleGraph = await this.BuildGraphAsync(groupName);
            }
            catch (Exception graphError)
            {
                output.Add(graphError);
                return output;
            }

            foreach (var rootNode in ruleGraph)
            {
                try
                {
                    await this.RunOnNodeAsync(rootNode, item);
                }
                catch (Exception runError)
                {
                    output.Add(runError);
                }
            }

            return output;
        }

        public async Task<TagsetCollection<TItem>> RunAsync<TItem>(string groupName, params TItem[] inputItems)
        {
            if (string.IsNullOrWhiteSpace(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (null == inputItems || 0 == inputItems.Length) throw new RuleEngineException("No inputs defined or input is null.");

            TagsetCollection<TItem> tagsetCollection = new TagsetCollection<TItem>();

            var ruleGraph = await this.BuildGraphAsync(groupName);

            foreach (var inputItem in inputItems)
            {
                foreach (var rootNode in ruleGraph)
                {
                    if (await this.RunOnNodeAsync(rootNode, inputItem))
                    {
                        tagsetCollection.AddItem(rootNode.Value.Tag, inputItem);
                    }
                }
            }

            return tagsetCollection;
        }
    }
}
