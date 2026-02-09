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
    
    public void Push(GraphNodeInitializationRequest request)
    {
        if (CXComponentGraph.CreateFromInitializationRequest(request, GraphContext) is { } node)
        {
            GraphNode.Children.Add(node);
        }
    }

    public void Push<T>(
        T component,
        ICXNode? cxNode = null,
        IReadOnlyList<CXNode>? children = null,
        GraphNode? parent = null
    ) where T : IComponentNode
        => Push(new(component, cxNode, parent, children));
}