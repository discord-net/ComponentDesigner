using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public sealed record InterpolationState(
    GraphNode GraphNode,
    ICXNode CXNode,
    int InterpolationId
) : ComponentState(GraphNode, CXNode);

public sealed class InterpolationComponentNode : ComponentNode<InterpolationState>, IDynamicComponentNode
{
    public override string Name { get; } = "<interpolated component>";

    public override bool IsUserAccessible => false;

    public override InterpolationState? Initialize(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
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

    public override Result<RenderedComponent> Emit(
        InterpolationState state,
        ComponentEmitContext context,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    ) => context.Renderer.RenderInterpolation(
        context,
        context.GetInterpolationInfo(state.InterpolationId),
        options.TypingContext,
        cancellationToken
    );
}