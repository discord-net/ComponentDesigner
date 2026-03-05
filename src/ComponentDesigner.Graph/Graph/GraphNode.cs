using System.Diagnostics;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser.Util;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public sealed class GraphNode : IEquatable<GraphNode>, ISourceLocatable
{
    public int Id { get; }

    public CXTextSpan TextSpan => State.TextSpan;

    public GraphNode? Parent
        => _parentId.HasValue ? Tree[_parentId.Value] : null;

    public IComponentNode Component { get; }

    public ComponentState State { get; internal set; }

    public bool HasChildren => _children?.Count > 0;

    public IReadOnlyList<GraphNode> Children => _children ?? (IReadOnlyList<GraphNode>)[];

    internal readonly CXComponentTree Tree;

    private Result<RenderedComponent>? _result;

    private readonly int? _parentId;
    private NodeList? _children;

    internal GraphNode(
        CXComponentTree tree,
        int id,
        IComponentNode component,
        ComponentState? state = null,
        NodeList? children = null,
        int? parentId = null
    )
    {
        Id = id;
        Tree = tree;
        Component = component;
        _children = children;
        _parentId = parentId;
        State = state ?? new(this, null);

        if (Parent is { } parent)
        {
            parent._children ??= new(tree);
            parent._children.Add(this);
        }
    }

    internal bool RemoveChild(GraphNode child)
        => _children?.Remove(child) ?? false;

    public GraphNode Reuse(
        CXComponentTree tree,
        ComponentState? state = null
    ) => new(
        tree,
        Id,
        Component,
        state ?? State,
        _children?.WithTree(tree),
        _parentId
    );

    public Result<RenderedComponent> Render(
        ComponentEmitContext context,
        ComponentOptions options = default,
        CancellationToken cancellationToken = default
    ) => _result ??= Component.Render(context, State, options, cancellationToken);

    public bool Equals(GraphNode? other)
    {
        if (other is null) return false;

        return
            State.Equals(other.State) &&
            Component.Equals(other.Component) &&
            (
                (_children?.Count ?? 0) == (other._children?.Count ?? 0) &&
                (_children is null || _children.Equals(other._children))
            ) &&
            _parentId == other._parentId;
    }

    public override int GetHashCode()
        => Hash.Combine(
            Component,
            State,
            _children,
            _parentId
        );
}