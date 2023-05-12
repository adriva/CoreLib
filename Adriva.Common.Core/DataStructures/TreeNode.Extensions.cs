using System;
using System.Collections.Generic;
using System.Linq;

namespace Adriva.Common.Core.DataStructures
{
    public static class TreeNodeExtensions
    {
        public static LinkedList<T> Flatten<T>(this TreeNode<T> node)
        {
            LinkedList<T> output = new LinkedList<T>();
            Queue<TreeNode<T>> queue = new Queue<TreeNode<T>>();
            queue.Enqueue(node);

            while (0 < queue.Count)
            {
                var treeNode = queue.Dequeue();
                output.AddLast(treeNode.Value);

                foreach (var childNode in treeNode.Children)
                {
                    queue.Enqueue(childNode);
                }
            }

            return output;
        }

        public static IEnumerable<TreeNode<TValue>> CreateTree<TValue>(this IEnumerable<TValue> items, Func<TValue, long> idResolver, Func<TValue, long?> parentIdResolver)
        {
            if (null == items) return null;

            if (null == idResolver) throw new ArgumentNullException(nameof(idResolver));
            if (null == parentIdResolver) throw new ArgumentNullException(nameof(parentIdResolver));

            Dictionary<long, TreeNode<TValue>> itemsLookup = items.ToDictionary(idResolver, x => new TreeNode<TValue>(x));
            Dictionary<long, bool> processedLookup = new Dictionary<long, bool>();

            bool isNoop;

            do
            {
                isNoop = true;
                foreach (var nodePair in itemsLookup)
                {
                    long? parentId = parentIdResolver.Invoke(nodePair.Value.Value);

                    if (parentId.HasValue)
                    {
                        if (itemsLookup.ContainsKey(parentId.Value) && !processedLookup.ContainsKey(idResolver(nodePair.Value.Value)))
                        {
                            itemsLookup[parentId.Value].Children.Add(nodePair.Value);
                            processedLookup[idResolver(nodePair.Value.Value)] = true;
                            isNoop = false;
                        }
                    }
                }
            } while (!isNoop);

            var nonRootItems = itemsLookup.Where(x => parentIdResolver(x.Value.Value).HasValue).ToArray();

            foreach (var nonRootItem in nonRootItems)
            {
                itemsLookup.Remove(nonRootItem.Key);
            }

            processedLookup.Clear();
            return itemsLookup.Values;
        }
    }
}
