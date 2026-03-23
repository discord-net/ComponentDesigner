using ComponentDesigner.Nodes.TextControls;
using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public sealed record TextControlState : ComponentState
{
    public TextControlGraph TextControlGraph { get; init; }

    public override CXTextSpan TextSpan => TextControlGraph.TextSpan;

    public TextControlState(
        TextControlGraph graph,
        ComponentNodeInitializationContext context,
        CancellationToken cancellationToken
    ) : base(context, cancellationToken, graph.TextSpan)
    {
        TextControlGraph = graph;
    }

    public TextControlState(TextControlGraph graph, GraphNode graphNode)
        : base(graphNode)
    {
        TextControlGraph = graph;
    }
}

public sealed class TextControlNode : ComponentNode<TextControlState>
{
    public static readonly TextControlNode Instance = new();

    public override string Name => "<text controls>";

    public override bool IsUserAccessible => false;

    public override TextControlState? Initialize(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    )
    {
        if (context.CXNode is not CXCollection<CXNode> cxNodes) return null;

        using var enumerator = GraphNodeEnumerator
            .GetNext(cxNodes)
            .GetEnumerator();

        if (
            !enumerator.MoveNext() ||
            !TextControlElement.TryCreate(
                context.GraphContext,
                enumerator,
                diagnostics,
                out var graph,
                out var enumeratorHasMore,
                cancellationToken
            )
        )
        {
            return null;
        }

        // TODO: check remaining children

        return new TextControlState(
            graph,
            context,
            cancellationToken
        );
    }

    public override void Validate(
        IComponentContext context, TextControlState state, IDiagnosticBag bag,
        CancellationToken cancellationToken = default
    )
    {
        // no validation
    }

    public override Result<RenderedComponent> Render(
        ComponentEmitContext context, TextControlState state, ComponentOptions options,
        CancellationToken cancellationToken = default
    ) => context
        .Renderer
        .RenderTextControls(
            context,
            state.TextControlGraph,
            options.TypingContext,
            cancellationToken
        );
}