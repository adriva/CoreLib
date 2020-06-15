using System;
using System.Collections.Generic;
using System.Linq;
using Adriva.Common.Core.DataStructures;

namespace Adriva.Extensions.RuleEngine
{
    public static class RuleExtensions
    {
        public static IEnumerable<TreeNode<Rule>> BuildGraph(this IEnumerable<Rule> rules)
        {
            TreeNode<Rule> BreadthFirstSearch(TreeNode<Rule> node, Func<Rule, bool> predicate)
            {
                if (null == node || null == predicate) return null;

                Queue<TreeNode<Rule>> queue = new Queue<TreeNode<Rule>>();
                queue.Enqueue(node);

                while (0 < queue.Count)
                {
                    var current = queue.Dequeue();
                    if (predicate.Invoke(current.Value)) return current;
                    else
                    {
                        foreach (var child in current.Children)
                        {
                            queue.Enqueue(child);
                        }
                    }
                }

                return null;
            }

            if (null == rules || !rules.Any()) throw new ArgumentException(nameof(rules));

            Dictionary<long, TreeNode<Rule>> orphanRules = new Dictionary<long, TreeNode<Rule>>();

            List<TreeNode<Rule>> graphRoots = new List<TreeNode<Rule>>();

            var ruleEnumerator = rules.GetEnumerator();

            while (ruleEnumerator.MoveNext())
            {
                Rule current = ruleEnumerator.Current;

                if (null == current) continue;

                if (string.IsNullOrWhiteSpace(current.Tag) && !current.ParentId.HasValue)
                    throw new ArgumentException($"Rule {current.Id} doesn't have a tag associated. All root rules must have a tag name.");

                bool isAdded = false;

                if (!current.ParentId.HasValue)
                {
                    graphRoots.Add(new TreeNode<Rule>(current));
                    continue;
                }
                else
                {
                    foreach (var graphRoot in graphRoots)
                    {
                        TreeNode<Rule> parent = BreadthFirstSearch(graphRoot, (r) => r.Id == current.ParentId);
                        if (null != parent)
                        {
                            parent.Children.Add(new TreeNode<Rule>(current));
                            isAdded = true;
                            break;
                        }
                    }
                }

                if (!isAdded)
                {
                    if (orphanRules.ContainsKey(current.ParentId.Value))
                    {
                        orphanRules[current.ParentId.Value].Children.Add(new TreeNode<Rule>(current));
                    }
                    else
                    {
                        orphanRules.Add(current.ParentId.Value, new TreeNode<Rule>(current));
                    }
                }
            }

            if (0 < orphanRules.Count)
            {
                foreach (var orphanRule in orphanRules)
                {
                    foreach (var graphRoot in graphRoots)
                    {
                        TreeNode<Rule> parent = BreadthFirstSearch(graphRoot, (r) => r.Id == orphanRule.Key);
                        if (null != parent)
                        {
                            parent.Children.Add(orphanRule.Value);
                            break;
                        }
                    }
                }
            }

            return graphRoots;
        }
    }
}
