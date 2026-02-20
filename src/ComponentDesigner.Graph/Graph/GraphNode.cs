using System.Diagnostics;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser.Util;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public sealed class GraphNode : IEquatable<GraphNode>, ISourceLocatable
{
    public CXTextSpan TextSpan => State.TextSpan;
    public GraphNode? Parent { get; private set; }
    public IComponentNode Component { get; }

    public ComponentState State
    {
        get => _state ?? throw new InvalidOperationException("Attempt to access node state before initialization");
        set => _state = value;
    }

    public bool HasChildren => _children?.Count > 0;

    public List<GraphNode> Children => _children ??= [];
    public List<GraphNode> Attributes => _attributes ??= [];


    private List<GraphNode>? _children;
    private List<GraphNode>? _attributes;
    private ComponentState? _state;

    private Result<RenderedComponent>? _result;

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

    public GraphNode Update(
        IComponentContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken,
        GraphNode? parent = null
    )
    {
        var newState = _state is null
            ? _state
            : Component.UpdateState(_state, context, diagnostics, cancellationToken);

        var result = new GraphNode(Component, newState, parent: parent);

        UpdateNodes(ref result._children, _children);
        UpdateNodes(ref result._attributes, _attributes);

        return result;

        void UpdateNodes(ref List<GraphNode>? results, List<GraphNode>? nodes)
        {
            if (nodes is null or { Count: 0 }) return;

            results ??= [];

            for (var i = 0; i < nodes.Count; i++)
            {
                results[i] = nodes[i].Update(context, diagnostics, cancellationToken, result);
            }
        }
    }

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