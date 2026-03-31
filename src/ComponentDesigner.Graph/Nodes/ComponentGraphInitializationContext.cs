using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public readonly record struct GraphNodeInitializationRequest(
    IComponentNode Component,
    ICXNode? CXNode = null,
    GraphNode? Parent = null,
    IReadOnlyList<ICXNode>? Children = null
);

public readonly struct ComponentGraphInitializationContext(
    GraphNode? parentGraphNode,
    ICXNode? cxNode,
    IGraphContext graphContext
)
{
    public readonly GraphNode? ParentGraphNode = parentGraphNode;
    public readonly ICXNode? CXNode = cxNode;
    public readonly IGraphContext GraphContext = graphContext;

    public void Push(GraphNodeInitializationRequest request, CancellationToken cancellationToken = default)
        => GraphContext.Push(request, cancellationToken);

    public void Push<T>(
        T component,
        ICXNode? cxNode = null,
        IReadOnlyList<ICXNode>? children = null,
        GraphNode? parent = null,
        CancellationToken cancellationToken = default
    ) where T : IComponentNode
        => Push(new(component, cxNode, parent, children), cancellationToken);
}