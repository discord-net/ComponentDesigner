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
    GraphInitializationContext graphInitializationContext
)
{
    public IDiagnosticBag Diagnostics => GraphInitializationContext.Diagnostics;

    public readonly GraphNode? ParentGraphNode = parentGraphNode;
    public readonly ICXNode? CXNode = cxNode;
    public readonly GraphInitializationContext GraphInitializationContext = graphInitializationContext;

    public void Push(GraphNodeInitializationRequest request)
        => CXComponentGraph.CreateFromInitializationRequest(request, GraphInitializationContext);

    public void Push<T>(
        T component,
        ICXNode? cxNode = null,
        IReadOnlyList<ICXNode>? children = null,
        GraphNode? parent = null
    ) where T : IComponentNode
        => Push(new(component, cxNode, parent, children));
}