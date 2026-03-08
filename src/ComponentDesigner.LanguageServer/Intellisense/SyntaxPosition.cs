using ComponentDesigner.Parser;

namespace Discord.ComponentDesigner.LanguageServer;

public static class SyntaxPosition
{
    public static ICXNode? Get(CXDocument document, int position)
    {
        if (!document.TextSpan.Contains(position)) return null;

        return Pick(document.Slots, position) ?? document;
    }

    private static ICXNode? Pick(IReadOnlyList<ICXNode> nodes, int position)
    {
        if (nodes.Count is 0) return null;

        if (nodes.Count is 1)
        {
            if (nodes[0].TextSpan.Contains(position))
                return Choose(nodes[0], position);

            return null;
        }
        
        var low = 0;
        var high = nodes.Count - 1;

        while (low <= high)
        {
            var mid = low + ((high - low) >> 1);
            var node = nodes[mid];

            if (node.TextSpan.Contains(position))
                return Choose(node, position);

            if (node.TextSpan.Start > position)
            {
                high = mid - 1;
                continue;
            }

            low = mid + 1;
        }

        var index = ~low;

        if (index < 0 || index >= nodes.Count) return null;

        return Choose(nodes[index], position);

        static ICXNode? Choose(ICXNode node, int position)
            => Pick(node.Slots, position) ?? node;
    }
}