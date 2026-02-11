using ComponentDesigner;
using ComponentDesigner.Nodes;

namespace Discord.ComponentDesigner;

partial class DiscordNetRenderer
{
    public override bool IsValidComponentType(
        IComponentContext context,
        ICSharpTypeSymbol? symbol,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }
}