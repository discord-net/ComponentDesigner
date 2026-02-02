using System.Diagnostics;
using Discord.CX.Nodes;
using Discord.CX.Util;

namespace Discord.CX;

public sealed class GraphNode : IEquatable<GraphNode>
{
    public GraphNode? Parent { get; }
    public IComponentNode Component { get; }

    public ComponentState State
    {
        get => _state ?? throw new InvalidOperationException("Attempt to access node state before initialization");
        set => _state = value;
    }

    public bool HasChildren => _children?.Count > 0;

    public IList<GraphNode> Children => _children ??= [];
    public IList<GraphNode> Attributes => _attributes ??= [];


    private List<GraphNode>? _children;
    private List<GraphNode>? _attributes;
    private ComponentState? _state;

    private Result<string>? _result;

    public GraphNode(
        IComponentNode component,
        ComponentState? state = null,
        List<GraphNode>? children = null,
        List<GraphNode>? attributes = null,
        GraphNode? parent = null
    )
    {
        Component = component;
        Parent = parent;
        _state = state;
        _children = children;
        _attributes = attributes;
    }


    public Result<string> Emit(
        ComponentEmitContext context,
        ComponentOptions options = default,
        CancellationToken token = default
    )
    {
        Debug.Assert(_state is not null, "State should not be null by build time");

        return _result ??= Component.Emit(State, context, options, token);
    }

    public bool Equals(GraphNode? other)
    {
        if (other is null) return false;

        return
            (_state?.Equals(other._state) ?? other._state is null) &&
            Component.Equals(other.Component) &&
            (_children, other._children) switch
            {
                (not null, not null) => _children.SequenceEqual(other._children),
                (null, null) => true,
                _ => false
            } &&
            (_attributes, other._attributes) switch
            {
                (not null, not null) => _attributes.SequenceEqual(other._attributes),
                (null, null) => true,
                _ => false
            };
    }

    public override int GetHashCode()
        => Hash.Combine(
            Component,
            _state,
            _children?.Aggregate(0, Hash.Combine),
            _attributes?.Aggregate(0, Hash.Combine)
        );
}