using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public record ComponentState : ISourceLocatable
{
    public GraphNode GraphNode { get; init; }

    public ICXNode? CXNode { get; init; }

    public virtual CXTextSpan TextSpan
    {
        get
        {
            if (_textSpan.HasValue) return _textSpan.Value;

            if (CXNode is null)
            {
                if (Children.Count is not 0)
                {
                    return _textSpan ??= CXTextSpan.FromBounds(
                        Children[0].State.TextSpan.Start,
                        Children[Children.Count - 1].State.TextSpan.End
                    );
                }

                var current = this;

                while (current is not null && current.CXNode is null)
                    current = current.GraphNode?.Parent?.State;

                return _textSpan ??= current?.TextSpan ?? default;
            }
            else
            {
                return _textSpan ??= CXNode.TextSpan;
            }
        }
    }

    public CXTextSpan ElementIdentifierTextSpanOrBetter
        => CXNode is CXElement { OpeningTag.Identifier: { } identifier } ? identifier.TextSpan : TextSpan;

    public bool HasGraphChildren => GraphNode.HasChildren;

    public IReadOnlyList<GraphNode> Children => GraphNode.Children;

    [MemberNotNullWhen(true, nameof(CXNode))]
    public bool IsSourcedFromElement => CXNode is CXElement;

    public bool IsRootNode => GraphNode.Parent is null;

    public virtual IReadOnlyList<ComponentProperty> Properties => GraphNode.Component.Properties;

    [field: MaybeNull]
    public ComponentPropertyValueSource ChildSource
        => field ??= new ComponentPropertyValueSource.Child(GraphNode);

    private CXTextSpan? _textSpan;
    private Dictionary<ComponentProperty, ComponentPropertyValue>? _propertyValues;

    public ComponentState(
        GraphNode graphNode,
        ICXNode? cxNode,
        ComponentNodeInitializationContext context,
        CancellationToken cancellationToken,
        CXTextSpan? textSpan = null,
        bool initialize = true
    )
    {
        _textSpan = textSpan;
        GraphNode = graphNode;
        CXNode = cxNode;

        if(initialize) Initialize(context, cancellationToken);
    }

    public ComponentState(
        GraphNode graphNode
    )
    {
        GraphNode = graphNode;
        CXNode = null;
    }

    public ComponentState(
        ComponentNodeInitializationContext context,
        CancellationToken cancellationToken,
        CXTextSpan? textSpan = null,
        bool initialize = true
    ) : this(context.GraphNode, context.CXNode, context, cancellationToken, textSpan, initialize)
    {
    }

    protected ComponentState(ComponentState other)
    {
        GraphNode = other.GraphNode;
        CXNode = other.CXNode;
        _propertyValues = other._propertyValues;
        _textSpan = other._textSpan;
    }

    public virtual bool TryGetProperty(
        string name, [MaybeNullWhen(false)] out ComponentProperty property
    ) => GraphNode.Component.TryGetProperty(name, out property);

    protected void Initialize(ComponentNodeInitializationContext context, CancellationToken cancellationToken)
    {
        if (CXNode is not CXElement element) return;

        _propertyValues ??= [];

        foreach (var attribute in element.Attributes)
        {
            if (!TryGetProperty(attribute.Identifier, out var property)) continue;

            var source = new ComponentPropertyValueSource.Attribute(
                attribute
            );

            _propertyValues[property] = BuildPropertyValueFromSyntax(
                context,
                property,
                source,
                attribute.Value,
                attribute.Value?.TextSpan ?? TextSpan,
                cancellationToken
            );
        }
    }


    internal ComponentPropertyValue BuildPropertyValueFromSyntax(
        ComponentNodeInitializationContext context,
        ComponentProperty property,
        ComponentPropertyValueSource source,
        CXValue? cxValue,
        CXTextSpan textSpan,
        CancellationToken cancellationToken
    )
    {
        switch (cxValue)
        {
            case CXValue.Element:
            {
                var graphNodes = GraphNode
                    .Children
                    .Where(x =>
                        x.State.CXNode is not null &&
                        textSpan.Contains(x.State.CXNode.TextSpan)
                    )
                    .ToArray();

                return FromGraphNodes(graphNodes);
            }
            case CXValue.Interpolation interpolation
                when context.GraphContext.IsInterpolatedComponent(interpolation, cancellationToken):
                return FromGraphNodes(
                    context.PushAsChildren(interpolation, cancellationToken)
                );
            default:
                if (
                    BuildPropertyValueFromSimpleSyntax(
                        context.GraphContext,
                        property,
                        source,
                        cxValue,
                        textSpan,
                        cancellationToken
                    ) is not { } value
                )
                {
                    // TODO: this means the syntax node was a component, but we didn't handle it in this function
                    Debug.Fail("Didn't handle case for component");
                    return new ComponentPropertyValue.None(
                        source,
                        property,
                        ComponentPropertyLocationInfo.From(textSpan, cxValue)
                    );
                }

                return value;
        }

        ComponentPropertyValue FromGraphNodes(IReadOnlyList<GraphNode> graphNodes)
        {
            return graphNodes.Count switch
            {
                0 => new ComponentPropertyValue.None(source, property, textSpan),
                1 when property.ValueCardinalityOfOne => new ComponentPropertyValue.Component(source, property, graphNodes[0]),
                _ => new ComponentPropertyValue.Many(
                    source,
                    property,
                    textSpan,
                    [.. graphNodes.Select(x => new ComponentPropertyValue.Component(source, property, x))]
                )
            };
        }
    }

    internal static ComponentPropertyValue? BuildPropertyValueFromSimpleSyntax(
        IComponentContext context,
        ComponentProperty property,
        ComponentPropertyValueSource source,
        CXValue? cxValue,
        CXTextSpan textSpan,
        CancellationToken cancellationToken
    )
    {
        switch (cxValue)
        {
            case CXValue.Scalar scalar:
                return new ComponentPropertyValue.Literal(
                    source,
                    property,
                    ComponentPropertyLocationInfo.From(textSpan, cxValue),
                    scalar.Value
                );

            case CXValue.Interpolation interpolation:
            {
                if (context.IsInterpolatedComponent(interpolation, cancellationToken))
                {
                    return null;
                }

                return new ComponentPropertyValue.Interpolation(
                    source,
                    property,
                    ComponentPropertyLocationInfo.From(textSpan, cxValue),
                    context.GetInterpolationInfo(interpolation)
                );
            }

            case CXValue.Multipart multipart:
            {
                using var _ = List<ComponentPropertyValue>.Pooled(out var parts);
                parts.Clear();

                foreach (var token in multipart.Tokens)
                {
                    switch (token.Kind)
                    {
                        case CXTokenKind.Text:
                            parts.Add(
                                new ComponentPropertyValue.Literal(
                                    source,
                                    property,
                                    ComponentPropertyLocationInfo.From(textSpan, token),
                                    token.Value
                                )
                            );
                            continue;
                        case CXTokenKind.Interpolation when token.InterpolationIndex is { } index:
                            parts.Add(
                                new ComponentPropertyValue.Interpolation(
                                    source,
                                    property,
                                    ComponentPropertyLocationInfo.From(textSpan, token),
                                    context.GetInterpolationInfo(index)
                                )
                            );
                            continue;
                    }
                }

                return parts.Count switch
                {
                    0 => new ComponentPropertyValue.None(
                        source,
                        property,
                        ComponentPropertyLocationInfo.From(textSpan, cxValue)
                    ),
                    1 when property.ValueCardinalityOfOne => parts[0],
                    _ => new ComponentPropertyValue.Many(
                        source,
                        property,
                        ComponentPropertyLocationInfo.From(textSpan, cxValue),
                        [..parts]
                    )
                };
            }

            default:
                return new ComponentPropertyValue.None(
                    source,
                    property,
                    ComponentPropertyLocationInfo.From(textSpan, cxValue)
                );
        }
    }

    public ComponentPropertyValue GetPropertyValue(ComponentProperty property)
    {
        _propertyValues ??= [];

        if (!_propertyValues.TryGetValue(property, out var value))
            _propertyValues[property] = value = new ComponentPropertyValue.None(
                ComponentPropertyValueSource.None,
                property,
                TextSpan
            );

        return value;
    }

    internal void SetPropertyValueToChildren(ComponentProperty property)
        => SetPropertyValueToChildren(
            property,
            GraphNode
                .Children
        );

    internal void SetPropertyValueToChildren(
        ComponentProperty property,
        params IReadOnlyList<GraphNode> children
    )
    {
        _propertyValues ??= [];

        _propertyValues[property] = children.Count switch
        {
            0 => new ComponentPropertyValue.None(ChildSource, property, TextSpan),
            1 when property.ValueCardinalityOfOne => new ComponentPropertyValue.Component(
                ChildSource,
                property,
                children[0]
            ),
            _ => new ComponentPropertyValue.Many(
                ChildSource,
                property,
                [
                    .. children.Select(x => new ComponentPropertyValue.Component(
                        ChildSource,
                        property,
                        x
                    ))
                ]
            )
        };
    }

    internal void SetPropertyValueToChild(
        ComponentProperty property,
        GraphNode child
    )
    {
        _propertyValues ??= [];
        _propertyValues[property] = new ComponentPropertyValue.Component(ChildSource, property, child);
    }

    internal void SetPropertyValueToChild(
        ComponentProperty property,
        ICXNode child
    )
    {
        var childGraphNode = Children
            .FirstOrDefault(x => ReferenceEquals(child, x.State.CXNode));

        if (childGraphNode is null) return;

        SetPropertyValueToChild(property, childGraphNode);
    }

    internal void SetPropertyValue(
        ComponentNodeInitializationContext context,
        ComponentProperty property,
        CXValue value,
        CancellationToken cancellationToken = default
    )
    {
        _propertyValues ??= [];

        var source = ComponentPropertyValueSource.None;

        if (CXNode is CXElement element)
        {
            if (element.Children.TextSpan.Contains(value.TextSpan))
                source = ChildSource;
            else if (
                element.Attributes.TextSpan.Contains(value.TextSpan) &&
                value.FirstAncestorOfTypeOrDefault<CXAttribute>() is { } attribute
            )
            {
                source = new ComponentPropertyValueSource.Attribute(attribute);
            }
        }

        _propertyValues[property] = BuildPropertyValueFromSyntax(
            context,
            property,
            source,
            value,
            value.TextSpan,
            cancellationToken
        );
    }

    internal void SetPropertyValue(ComponentProperty property, ComponentPropertyValue value)
    {
        _propertyValues ??= [];
        _propertyValues[property] = value;
    }

    // internal void IngestChildrenAsScalarValueForProperty(
    //     ComponentProperty property
    // )
    // {
    //     if (CXNode is not CXElement { Children.Count: > 0 } element) return;
    //
    //     if (element.Children[0] is not CXValue value)
    //     {
    //         // TODO: maybe report diagnostic?
    //         return;
    //     }
    //
    //     SetPropertyValue(property, value);
    // }

    public virtual bool Equals(ComponentState? other)
        => other is not null && (
            ReferenceEquals(this, other)
            ||
            (CXNode?.Equals(other.CXNode!) ?? other.CXNode is null)
        );

    public override int GetHashCode()
        => CXNode?.GetHashCode() ?? 0;
}