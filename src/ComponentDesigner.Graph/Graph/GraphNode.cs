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

    public ComponentState State
    {
        get => _state ?? throw new InvalidOperationException("Attempt to access node state before initialization");
        set => _state = value;
    }

    public bool HasChildren => _children?.Count > 0;

    public IReadOnlyList<GraphNode> Children => _children ?? (IReadOnlyList<GraphNode>)[];

    private ComponentState? _state;

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
        _state = state;

        if (Parent is { } parent)
        {
            parent._children ??= new(tree);
            parent._children.Add(this);
        }
    }

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

    public Result<RenderedComponent> Emit(
        ComponentEmitContext context,
        ComponentOptions options = default,
        CancellationToken cancellationToken = default
    )
    {
        Debug.Assert(_state is not null, "State should not be null by build time");

        return _result ??= Component.Emit(State, context, options, cancellationToken);
    }

    public bool Equals(GraphNode? other)
    {
        if (other is null) return false;

        return
            (_state?.Equals(other._state) ?? other._state is null) &&
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
            _state,
            _children,
            _parentId
        );
}