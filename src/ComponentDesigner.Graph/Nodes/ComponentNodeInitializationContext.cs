using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public readonly struct ComponentNodeInitializationContext
{
    public ICXModel CX => GraphContext.CX;
    public ICompilationProvider CompilationProvider => GraphContext.CompilationProvider;
    public IComponentTypingProvider? ComponentTypingProvider => GraphContext.ComponentTypingProvider;

    public readonly GraphNode GraphNode;
    public readonly ICXNode? CXNode;
    public readonly GraphInitializationContext GraphContext;

    public ComponentNodeInitializationContext(
        ICXNode? cxNode,
        GraphNode graphNode,
        GraphInitializationContext context
    )
    {
        GraphNode = graphNode;
        CXNode = cxNode;
        GraphContext = context;
    }

    public void AddChild(ICXNode cxNode, CancellationToken cancellationToken = default)
    {
        CXComponentGraph.CreateNodes(
            cxNode,
            GraphNode,
            GraphContext,
            cancellationToken
        );
    }

    public GraphNode? Push(
        GraphNodeInitializationRequest request,
        CancellationToken cancellationToken = default
    ) => CXComponentGraph.CreateFromInitializationRequest(request, GraphContext, cancellationToken);

    public IReadOnlyList<GraphNode> PushAsChildren(
        CXElement element,
        CancellationToken cancellationToken = default
    )
    {
        var start = GraphNode.Children.Count;

        CXComponentGraph.CreateElementNodes(
            element,
            GraphNode,
            GraphContext,
            cancellationToken
        );

        var end = GraphNode.Children.Count;

        if (start == end) return [];

        return [..GraphNode.Children.Skip(start).Take(end - start)];
    }

    public IReadOnlyList<GraphNode> PushAsChildren<T>(
        CXCollection<T> syntaxNodes,
        CancellationToken cancellationToken = default
    ) where T : class, ICXNode
        => PushAsChildren((IReadOnlyList<ICXNode>)syntaxNodes, cancellationToken);
    
    public IReadOnlyList<GraphNode> PushAsChildren(
        IReadOnlyList<ICXNode> syntaxNodes,
        CancellationToken cancellationToken = default
    )
    {
        var start = GraphNode.Children.Count;

        CXComponentGraph.CreateNodes(
            syntaxNodes,
            GraphNode,
            GraphContext,
            cancellationToken
        );

        var end = GraphNode.Children.Count;

        if (start == end) return [];

        return [..GraphNode.Children.Skip(start).Take(end - start)];
    }
    
    public IReadOnlyList<GraphNode> PushAsChildren(
        ICXNode syntaxNode,
        CancellationToken cancellationToken = default
    )
    {
        var start = GraphNode.Children.Count;

        CXComponentGraph.CreateNodes(
            syntaxNode,
            GraphNode,
            GraphContext,
            cancellationToken
        );

        var end = GraphNode.Children.Count;

        if (start == end) return [];

        return [..GraphNode.Children.Skip(start).Take(end - start)];
    }

    public GraphNode? Push<T>(
        T component,
        ICXNode? cxNode = null,
        IReadOnlyList<CXNode>? children = null,
        GraphNode? parent = null,
        CancellationToken cancellationToken = default
    ) where T : IComponentNode
        => Push(new(component, cxNode, parent, children), cancellationToken);
}