using Discord.CX.Parser;

namespace Discord.CX.Nodes;

public sealed record InterpolationState(
    GraphNode GraphNode,
    ICXNode CXNode,
    int InterpolationId
) : ComponentState(GraphNode, CXNode);

public sealed class InterpolationComponentNode : ComponentNode<InterpolationState>, IDynamicComponentNode
{
    public override string Name { get; } = "<interpolated component>";

    public override InterpolationState? Initialize(
        ComponentNodeInitializationContext context,
        IList<Diagnostic> diagnostics,
        CancellationToken cancellationToken = default
    )
    {
        var id = context.CXNode switch
        {
            CXValue.Interpolation interpolation => interpolation.InterpolationIndex,
            CXToken { Kind: CXTokenKind.Interpolation } token => token.InterpolationIndex,
            _ => null
        };

        if (id is null) return null;

        return new(context.GraphNode, context.CXNode!, id.Value);
    }

    public override Result<string> Emit(
        InterpolationState state,
        ComponentEmitContext context,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    )
    {
        /*
         * TODO: figure out how typing support is integrated for this, as different implementations may want to
         *       handle different component types etc
         */

        var info = context.GetInterpolationInfo(state.InterpolationId);

        return context.GetReferenceToDesignerValue(info, info.Symbol);
    }
}