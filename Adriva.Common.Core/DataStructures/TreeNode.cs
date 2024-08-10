using System.Collections.Generic;

namespace Adriva.Common.Core.DataStructures
{
    public class TreeNode<T>
    {
        public T Value { get; private set; }

        public IList<TreeNode<T>> Children { get; private set; }

        public TreeNode()
        {
            this.Children = new List<TreeNode<T>>();
        }

        public TreeNode(T value) : this()
        {
            this.Value = value;
        }
    }

}
