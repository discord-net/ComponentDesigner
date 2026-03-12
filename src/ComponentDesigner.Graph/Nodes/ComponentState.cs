using System.Diagnostics.CodeAnalysis;
using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public record ComponentState(
    GraphNode GraphNode,
    ICXNode? CXNode
) : ISourceLocatable
{
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

    [field: MaybeNull]
    public ComponentPropertyValueSource ChildSource
        => field ??= new ComponentPropertyValueSource.Child(GraphNode);

    private CXTextSpan? _textSpan;
    private Dictionary<ComponentProperty, ComponentPropertyValue>? _propertyValues;

    public ComponentState(ComponentNodeInitializationContext context) : this(context.GraphNode, context.CXNode)
    {
    }

    public void Initialize(ComponentNodeInitializationContext context, CancellationToken cancellationToken)
    {
        if (CXNode is not CXElement element) return;

        _propertyValues ??= [];

        foreach (var attribute in element.Attributes)
        {
            if (!GraphNode.Component.TryGetProperty(attribute.Identifier, out var property)) continue;

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
            case CXValue.Scalar scalar:
                return new ComponentPropertyValue.Literal(
                    source,
                    property,
                    textSpan,
                    scalar.Value
                );

            case CXValue.Interpolation interpolation:
            {
                if (context.GraphContext.IsInterpolatedComponent(interpolation, cancellationToken))
                {
                    return FromGraphNodes(
                        context.PushAsChildren(interpolation, cancellationToken)
                    );
                }

                return new ComponentPropertyValue.Interpolation(
                    source,
                    property,
                    textSpan,
                    context.GraphContext.GetInterpolationInfo(interpolation)
                );
            }

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
                                new ComponentPropertyValue.Literal(source, property, token.TextSpan, token.Value)
                            );
                            continue;
                        case CXTokenKind.Interpolation when token.InterpolationIndex is { } index:
                            parts.Add(
                                new ComponentPropertyValue.Interpolation(
                                    source,
                                    property,
                                    token.TextSpan,
                                    context.GraphContext.GetInterpolationInfo(index)
                                )
                            );
                            continue;
                    }
                }

                return parts.Count switch
                {
                    0 => new ComponentPropertyValue.None(source, property, multipart.TextSpan),
                    1 when !property.Kind.HasFlag(ComponentPropertyValueKind.Many) => parts[0],
                    _ => new ComponentPropertyValue.Many(
                        source,
                        property,
                        textSpan,
                        [..parts]
                    )
                };
            }

            default:
                return new ComponentPropertyValue.None(
                    source,
                    property,
                    textSpan
                );
        }

        ComponentPropertyValue FromGraphNodes(IReadOnlyList<GraphNode> graphNodes)
        {
            switch (graphNodes.Count)
            {
                case 0: return new ComponentPropertyValue.None(source, property, textSpan);

                case 1:
                    ComponentPropertyValue value = new ComponentPropertyValue.Component(
                        source,
                        property,
                        graphNodes[0]
                    );

                    if (property.Kind.HasFlag(ComponentPropertyValueKind.Many))
                        value = new ComponentPropertyValue.Many(
                            source,
                            property,
                            textSpan,
                            [value]
                        );

                    return value;

                default:
                    return new ComponentPropertyValue.Many(
                        source,
                        property,
                        textSpan,
                        [
                            .. graphNodes
                                .Select(x => new ComponentPropertyValue
                                    .Component(
                                        source,
                                        property,
                                        x
                                    )
                                )
                        ]
                    );
            }
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