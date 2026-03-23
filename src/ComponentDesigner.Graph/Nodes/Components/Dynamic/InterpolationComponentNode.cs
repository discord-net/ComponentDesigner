using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public sealed record InterpolationState : ComponentState
{
    public int InterpolationId { get; init; }

    public InterpolationState(
        int interpolationId,
        ComponentNodeInitializationContext context,
        CancellationToken cancellationToken
    ) : base(context, cancellationToken)
    {
        InterpolationId = interpolationId;
    }
}

public sealed class InterpolationComponentNode : ComponentNode<InterpolationState>, IDynamicComponentNode
{
    public static readonly InterpolationComponentNode Instance = new();

    public override string Name { get; } = "<interpolated component>";

    public override bool IsUserAccessible => false;

    public override bool HasExternalDependencies => true;

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

        return new(id.Value, context, cancellationToken);
    }

    public override void Validate(
        IComponentContext context, InterpolationState state, IDiagnosticBag bag,
        CancellationToken cancellationToken = default
    )
    {
        // no validation
    }

    public override Result<RenderedComponent> Render(
        ComponentEmitContext context,
        InterpolationState state,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    ) => context
        .Renderer
        .RenderInterpolation(
            context,
            context.GetInterpolationInfo(state.InterpolationId),
            options.TypingContext,
            cancellationToken
        );
}