using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public readonly struct ComponentNodeInitializationContext
{
    public ICXModel CX => GraphContext.CX;
    public ICompilationProvider CompilationProvider => GraphContext.CompilationProvider;
    public IComponentTypingProvider? ComponentTypingProvider => GraphContext.ComponentTypingProvider;

    public readonly GraphNode GraphNode;
    public readonly ICXNode? CXNode;
    public readonly IGraphContext GraphContext;

    public ComponentNodeInitializationContext(
        ICXNode? cxNode,
        GraphNode graphNode,
        IGraphContext context
    )
    {
        GraphNode = graphNode;
        CXNode = cxNode;
        GraphContext = context;
    }

    public void AddChild(ICXNode cxNode, CancellationToken cancellationToken = default)
    {
        GraphContext.Push(
            GraphNode,
            [cxNode],
            cancellationToken
        );
    }

    public GraphNode? Push(
        GraphNodeInitializationRequest request,
        CancellationToken cancellationToken = default
    ) => GraphContext.Push(request, cancellationToken);

    public IReadOnlyList<GraphNode> PushAsChildren(
        CXElement element,
        CancellationToken cancellationToken = default
    ) => GraphContext.Push(GraphNode, [element], cancellationToken);

    public IReadOnlyList<GraphNode> PushAsChildren<T>(
        CXCollection<T> syntaxNodes,
        CancellationToken cancellationToken = default
    ) where T : class, ICXNode
        => PushAsChildren((IReadOnlyList<ICXNode>)syntaxNodes, cancellationToken);

    public IReadOnlyList<GraphNode> PushAsChildren(
        IReadOnlyList<ICXNode> syntaxNodes,
        CancellationToken cancellationToken = default
    ) => GraphContext.Push(GraphNode, syntaxNodes, cancellationToken);

    public IReadOnlyList<GraphNode> PushAsChildren(
        ICXNode syntaxNode,
        CancellationToken cancellationToken = default
    ) => PushAsChildren([syntaxNode], cancellationToken);

    public GraphNode? Push<T>(
        T component,
        ICXNode? cxNode = null,
        IReadOnlyList<CXNode>? children = null,
        GraphNode? parent = null,
        CancellationToken cancellationToken = default
    ) where T : IComponentNode
        => Push(new(component, cxNode, parent, children), cancellationToken);
}