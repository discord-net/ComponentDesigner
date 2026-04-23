using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public sealed record InterpolationState : ComponentState
{
    public int InterpolationId { get; init; }

    public ICSharpTypeSymbol Symbol { get; init; }

    public InterpolationState(
        ICSharpTypeSymbol symbol,
        int interpolationId,
        ComponentNodeInitializationContext context,
        CancellationToken cancellationToken
    ) : base(context, cancellationToken)
    {
        Symbol = symbol;
        InterpolationId = interpolationId;
    }
}

public sealed class InterpolationComponentNode : ComponentNode<InterpolationState>, IDynamicComponentNode
{
    public static readonly InterpolationComponentNode Instance = new();

    public override string Name { get; } = "<interpolated component>";

    public override bool IsUserAccessible => false;

    public override bool HasExternalDependencies => true;

    public override InterpolationState? CreateState(
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

        if (context.ComponentTypingProvider is null)
        {
            diagnostics.Add(
                Diagnostic
                    .TypedComponentsAreNotSupported(context.GraphContext.Implementation)
                    .At(context.CXNode!)
            );

            return null;
        }

        var info = context.GraphContext.GetInterpolationInfo(id.Value);

        if (!context.ComponentTypingProvider.IsValidComponentType(context.GraphContext, info.Symbol, cancellationToken))
        {
            diagnostics.Add(
                Diagnostic
                    .NotAComponentType(info.Symbol!)
                    .At(context.CXNode!)
            );

            return null;
        }

        return new(
            info.Symbol!,
            info.Id,
            context,
            cancellationToken
        );
    }

    public override InterpolationState? UpdateState(
        InterpolationState state,
        IComponentContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    )
    {
        var info = context.GetInterpolationInfo(state.InterpolationId);

        if (context.ComponentTypingProvider is null) return null;

        if (
            !context.ComponentTypingProvider.IsValidComponentType(
                context,
                info.Symbol,
                cancellationToken
            )
        )
        {
            // the symbol is no longer a component
            return null;
        }

        return state with
        {
            Symbol = info.Symbol!
        };
    }

    public override void Validate(
        IComponentContext context, InterpolationState state, IDiagnosticBag bag,
        CancellationToken cancellationToken = default
    )
    {
        // no validation
    }
}