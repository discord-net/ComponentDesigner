using ComponentDesigner;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    public override bool IsValidComponentType(
        IComponentContext context,
        ICSharpTypeSymbol? symbol,
        CancellationToken cancellationToken = default
    )
    {
        // TODO
        return false;
    }
}