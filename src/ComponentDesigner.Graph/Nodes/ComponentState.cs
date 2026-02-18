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

    public IReadOnlyList<GraphNode> Children =>
        (GraphNode.Children as IReadOnlyList<GraphNode>) ?? [..GraphNode.Children];

    [MemberNotNullWhen(true, nameof(CXNode))]
    public bool IsSourcedFromElement => CXNode is CXElement;

    public bool IsRootNode => GraphNode.Parent is null;

    private CXTextSpan? _textSpan;
    private Dictionary<ComponentProperty, ComponentPropertyValue>? _propertyValues;

    public ComponentState(ComponentNodeInitializationContext context) : this(context.GraphNode, context.CXNode)
    {
    }

    public ComponentPropertyValue GetPropertyValue(ComponentProperty property)
    {
        if (_propertyValues?.TryGetValue(property, out var value) is true) return value;

        if (!IsSourcedFromElement)
            return new ComponentPropertyValue.Missing(property, TextSpan);

        _propertyValues ??= [];

        var element = (CXElement)CXNode;

        var attribute = element
            .Attributes
            .FirstOrDefault(x => property.MatchesName(x.Identifier));

        if (attribute is null)
            return _propertyValues[property] = new ComponentPropertyValue.Missing(property, TextSpan);

        if (attribute.Value is CXValue.Element attributeElement)
        {
            var graphNode = GraphNode
                ?.Attributes
                .FirstOrDefault(x => ReferenceEquals(x.State.CXNode, attributeElement.Value));

            return _propertyValues[property] = graphNode is null
                ? new ComponentPropertyValue.Missing(property, TextSpan)
                : new ComponentPropertyValue.AttributeComponent(property, attribute, graphNode);
        }

        return _propertyValues[property] = new ComponentPropertyValue.AttributeValue(
            property, attribute
        );
    }

    internal void SetPropertyValueToChildren(ComponentProperty property)
        => SetPropertyValueToChildren(
            property,
            GraphNode.Children
        );

    internal void SetPropertyValueToChildren(
        ComponentProperty property,
        params IReadOnlyList<GraphNode> children
    )
    {
        _propertyValues ??= [];

        _propertyValues[property] = new ComponentPropertyValue.Many(
            property,
            [..children.Select(x => new ComponentPropertyValue.Component(property, x))]
        );
    }

    internal void SetPropertyValueToChild(
        ComponentProperty property,
        GraphNode child
    )
    {
        _propertyValues ??= [];
        _propertyValues[property] = new ComponentPropertyValue.Component(property, child.State.TextSpan, child);
    }

    internal void SetPropertyValueToChild(
        ComponentProperty property,
        ICXNode child
    )
    {
        var childGraphNode = Children
            .FirstOrDefault(x => ReferenceEquals(child, x.State.CXNode));

        if (childGraphNode is null) return;

        _propertyValues ??= [];
        _propertyValues[property] = new ComponentPropertyValue.Component(
            property,
            childGraphNode
        );
    }

    internal void SetPropertyValue(ComponentProperty property, CXValue value)
    {
        _propertyValues ??= [];
        _propertyValues[property] = new ComponentPropertyValue.SyntaxValue(property, value);
    }

    internal void SetPropertyValue(ComponentProperty property, ComponentPropertyValue value)
    {
        _propertyValues ??= [];
        _propertyValues[property] = value;
    }

    internal void IngestChildrenAsScalarValueForProperty(
        ComponentProperty property
    )
    {
        if (CXNode is not CXElement { Children.Count: > 0 } element) return;

        if (element.Children[0] is not CXValue value)
        {
            // TODO: maybe report diagnostic?
            return;
        }

        SetPropertyValue(property, value);
    }

    public virtual bool Equals(ComponentState? other)
        => other is not null && (
            ReferenceEquals(this, other)
            ||
            (CXNode?.Equals(other.CXNode!) ?? other.CXNode is null)
        );

    public override int GetHashCode()
        => CXNode?.GetHashCode() ?? 0;
}