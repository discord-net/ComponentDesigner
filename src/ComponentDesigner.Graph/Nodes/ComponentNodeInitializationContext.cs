using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public readonly struct ComponentNodeInitializationContext
{
    public ICXModel CX => GraphContext.CX;
    public ICompilationProvider CompilationProvider => GraphContext.CompilationProvider;

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
            GraphNode.Children,
            cxNode,
            GraphNode,
            GraphContext,
            cancellationToken
        );
    }

    public GraphNode? Push(
        GraphNodeInitializationRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (CXComponentGraph.CreateFromInitializationRequest(request, GraphContext, cancellationToken) is { } node)
        {
            if (node.Parent is null) GraphNode.Children.Add(node);

            return node;
        }

        return null;
    }

    public IReadOnlyList<GraphNode> PushAsChildren(
        CXElement element,
        CancellationToken cancellationToken = default
    )
    {
        var start = GraphNode.Children.Count;

        CXComponentGraph.CreateElementNodes(
            GraphNode.Children,
            element,
            GraphNode,
            GraphContext,
            cancellationToken
        );

        var end = GraphNode.Children.Count;

        if (start == end) return [];

        return [..GraphNode.Children.Skip(start).Take(end - start)];
    }

    public IReadOnlyList<GraphNode> PushAsChildren(
        IReadOnlyList<ICXNode> syntaxNodes,
        CancellationToken cancellationToken = default
    )
    {
        var start = GraphNode.Children.Count;

        CXComponentGraph.CreateNodes(
            GraphNode.Children,
            syntaxNodes,
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