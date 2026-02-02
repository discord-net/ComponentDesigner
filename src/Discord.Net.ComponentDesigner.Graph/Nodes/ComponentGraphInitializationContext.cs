using Discord.CX.Parser;

namespace Discord.CX.Nodes;

public readonly record struct GraphNodeInitializationRequest(
    IComponentNode Component,
    ICXNode? CXNode = null,
    GraphNode? Parent = null,
    IReadOnlyList<CXNode>? Children = null
);

public readonly struct ComponentGraphInitializationContext
{
    public readonly GraphNode? ParentGraphNode;
    public readonly ICXNode? CXNode;
    public readonly GraphInitializationContext GraphInitializationContext;

    private readonly IList<GraphNode> _results;

    public ComponentGraphInitializationContext(
        GraphNode? parentGraphNode,
        ICXNode? cxNode,
        GraphInitializationContext graphInitializationContext,
        IList<GraphNode> results
    )
    {
        ParentGraphNode = parentGraphNode;
        CXNode = cxNode;
        GraphInitializationContext = graphInitializationContext;
        _results = results;
    }

    public void Push(GraphNodeInitializationRequest request)
    {
        if (CXGraph.CreateFromInitializationRequest(request, GraphInitializationContext) is { } node)
            _results.Add(node);
    }

    public void Push<T>(
        T component,
        ICXNode? cxNode = null,
        IReadOnlyList<CXNode>? children = null,
        GraphNode? parent = null
    ) where T : IComponentNode
        => Push(new(component, cxNode, parent, children));
}