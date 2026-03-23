using ComponentDesigner.Nodes;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public sealed class CXComponentTree : 
    IEquatable<CXComponentTree>
{
    public static CXComponentTree Empty => new();

    public int Count => _nodes.Count;
    public GraphNode this[int id] => _nodes[id];

    public IReadOnlyList<GraphNode> Nodes => _nodes;

    public bool HasExternalDependencies => _nodesWithExternalDependencies?.Count > 0;

    public IReadOnlyList<GraphNode> NodesWithExternalDependencies
        => _nodesWithExternalDependencies ?? [];

    public IReadOnlyList<GraphNode> RootNodes => _rootNodes ?? [];

    private readonly List<GraphNode> _nodes = [];

    private List<GraphNode>? _nodesWithExternalDependencies;
    private List<GraphNode>? _rootNodes;

    public GraphNode Reuse(
        GraphNode graphNode,
        ComponentState? state = null
    )
    {
        var newNode = graphNode.Reuse(this, state);

        _nodes.Add(newNode);

        if (newNode.Component.HasExternalDependencies)
        {
            _nodesWithExternalDependencies ??= [];
            _nodesWithExternalDependencies.Add(newNode);
        }

        return newNode;
    }

    public void DereferenceFromTree(GraphNode graphNode)
    {
        graphNode?.Parent?.RemoveChild(graphNode);
        _rootNodes?.Remove(graphNode!);
        _nodesWithExternalDependencies?.Remove(graphNode!);
    }

    public GraphNode Push(
        IComponentNode component,
        ComponentState? state = null,
        NodeList? children = null,
        GraphNode? parent = null
    )
    {
        var node = new GraphNode(
            this,
            _nodes.Count,
            component,
            state,
            children,
            parent?.Id
        );

        _nodes.Add(node);

        if (component.HasExternalDependencies)
        {
            _nodesWithExternalDependencies ??= [];
            _nodesWithExternalDependencies.Add(node);
        }

        if (parent is null)
        {
            _rootNodes ??= [];
            _rootNodes.Add(node);
        }

        return node;
    }

    public bool Equals(CXComponentTree? other)
        => other is not null && (
            ReferenceEquals(this, other) ||
            _nodes.SequenceEqual(other._nodes)
        );

    public override bool Equals(object? obj)
        => obj is CXComponentTree other && Equals(other);

    public override int GetHashCode()
        => _nodes.Aggregate(0, Hash.Combine);
}