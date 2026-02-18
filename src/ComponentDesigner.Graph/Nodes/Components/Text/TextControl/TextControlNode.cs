using ComponentDesigner.Nodes.TextControls;
using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public sealed record TextControlState(
    GraphNode GraphNode,
    ICXNode? CXNode,
    TextControlGraph TextControlGraph
) : ComponentState(GraphNode, CXNode)
{
    public override CXTextSpan TextSpan => TextControlGraph.TextSpan;
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

        return new TextControlState(context.GraphNode, cxNodes, graph);
    }

    public override Result<RenderedComponent> Emit(
        TextControlState state,
        ComponentEmitContext context,
        ComponentOptions options,
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