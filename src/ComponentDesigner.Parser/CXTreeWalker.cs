using System.Collections.Generic;

namespace ComponentDesigner.Parser;

/// <summary>
///     A utility class to walk the AST of a <see cref="ICXNode"/>.
/// </summary>
public static class CXTreeWalker
{
    /// <summary>
    ///     Walks the tree of the provided node.
    /// </summary>
    /// <param name="root">The node to walk</param>
    /// <returns>
    ///     A collection of nodes that were walked in a DFS style traversal, including the provided
    ///     <paramref name="root"/> node as the first element.
    /// </returns>
    public static IReadOnlyList<ICXNode> Walk(this ICXNode root)
    {
        var result = new List<ICXNode>();

        var stack = new Stack<(ICXNode Node, int SlotIndex)>([(root, 0)]);

        while (stack.Count > 0)
        {
            var (node, index) = stack.Pop();

            if (node is CXToken token)
            {
                result.Add(token);
                continue;
            }

            if (node is CXNode concreteNode)
            {
                if(index is 0) result.Add(node);

                if (concreteNode.Slots.Count > index)
                {
                    // enqueue self
                    stack.Push(
                        (concreteNode, index + 1)
                    );

                    // enqueue child
                    stack.Push(
                        (concreteNode.Slots[index], 0)
                    );

                    continue;
                }

                // we do nothing
            }
        }

        return result;
    }
}