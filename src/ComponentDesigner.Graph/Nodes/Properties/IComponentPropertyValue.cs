using System.Diagnostics.CodeAnalysis;
using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public abstract record ComponentPropertyValue(
    ComponentProperty Property,
    CXTextSpan TextSpan
)
{
    public string Name => Property.Name;

    public string UsedName => this switch
    {
        AttributeValue { Attribute.Identifier: var ident } => ident,
        AttributeElement { Attribute.Identifier: var ident } => ident,
        _ => Name
    };

    public bool HasValue => this
        is AttributeElement
        or AttributeValue { Attribute.Value: not null }
        or Children { GraphNodes.Count: > 0 }
        or SyntaxValue;

    public virtual CXValue? CXValue => null;

    public bool HasAttribute => this is AttributeValue or AttributeElement;

    public bool IsSpecified => this
        is AttributeElement { Attribute.IdentifierToken.IsMissing: false }
        or AttributeValue { Attribute.IdentifierToken.IsMissing: false }
        or Children
        or SyntaxValue;

    public bool IsOptional => Property.IsOptional;
    public bool RequiresValue => Property.RequiresValue;

    public bool TryGetLiteralValue([MaybeNullWhen(false)] out string value)
    {
        switch (CXValue)
        {
            case CXValue.Scalar scalar:
                value = scalar.Value;
                return true;
            case CXValue.StringLiteral { HasInterpolations: false } literal:
                value = literal.Tokens.ToValueString();
                return true;
        }

        value = null;
        return false;
    }

    public sealed record Missing(
        ComponentProperty Property,
        CXTextSpan TextSpan
    ) : ComponentPropertyValue(Property, TextSpan);

    public sealed record AttributeValue(
        ComponentProperty Property,
        CXAttribute Attribute
    ) : ComponentPropertyValue(Property, Attribute.Span)
    {
        public override CXValue? CXValue { get; } = Attribute.Value;
    }

    public sealed record AttributeElement(
        ComponentProperty Property,
        CXAttribute Attribute,
        GraphNode GraphNode
    ) : ComponentPropertyValue(Property, Attribute.Span);

    public sealed record SyntaxValue(
        ComponentProperty Property,
        CXValue CXValue
    ) : ComponentPropertyValue(Property, CXValue.Span)
    {
        public override CXValue CXValue { get; } = CXValue;
    }

    public sealed record Children(
        ComponentProperty Property,
        CXTextSpan TextSpan,
        IReadOnlyList<GraphNode> GraphNodes
    ) : ComponentPropertyValue(Property, TextSpan);
}