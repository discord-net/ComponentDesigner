using System.Diagnostics.CodeAnalysis;
using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public abstract record ComponentPropertyValue(
    ComponentProperty Property,
    CXTextSpan TextSpan
) : ISourceLocatable
{
    public ComponentPropertyValueKind Kind => this switch
    {
        AttributeComponent => ComponentPropertyValueKind.AttributeComponent,
        AttributeValue => ComponentPropertyValueKind.AttributeValue,
        Component => ComponentPropertyValueKind.Component,
        Many => ComponentPropertyValueKind.Many,
        SyntaxValue => ComponentPropertyValueKind.SyntaxValue,
        Missing => ComponentPropertyValueKind.Missing,
        _ => throw new ArgumentOutOfRangeException()
    };
    
    public string Name => Property.Name;

    public string UsedName => this switch
    {
        AttributeValue { Attribute.Identifier: var ident } => ident,
        AttributeComponent { Attribute.Identifier: var ident } => ident,
        _ => Name
    };

    public bool HasValue
        => this
            is AttributeComponent
            or AttributeValue { Attribute.Value: not null }
            or Component
            or SyntaxValue || (
            this is Many many && many.Values.All(x => x.HasValue)
        );

    public virtual CXValue? CXValue => null;

    public virtual GraphNode? GraphNode => null;  

    public bool HasAttribute => this is AttributeValue or AttributeComponent;

    public bool IsSpecified => this
        is AttributeComponent { Attribute.IdentifierToken.IsMissing: false }
        or AttributeValue { Attribute.IdentifierToken.IsMissing: false }
        or Component
        or SyntaxValue || (
        this is Many many && many.Values.All(x => x.IsSpecified)
    );

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
    ) : ComponentPropertyValue(Property, Attribute.TextSpan)
    {
        public override CXValue? CXValue { get; } = Attribute.Value;
    }

    public sealed record AttributeComponent(
        ComponentProperty Property,
        CXAttribute Attribute,
        GraphNode GraphNode
    ) : ComponentPropertyValue(Property, Attribute.TextSpan)
    {
        public override GraphNode GraphNode { get; } = GraphNode;
    }

    public sealed record SyntaxValue(
        ComponentProperty Property,
        CXValue CXValue
    ) : ComponentPropertyValue(Property, CXValue.TextSpan)
    {
        public override CXValue CXValue { get; } = CXValue;
    }

    public sealed record Many(
        ComponentProperty Property,
        IReadOnlyList<ComponentPropertyValue> Values
    ) : ComponentPropertyValue(Property, CXTextSpan.From(Values));

    public sealed record Component(
        ComponentProperty Property,
        CXTextSpan TextSpan,
        GraphNode GraphNode
    ) : ComponentPropertyValue(Property, TextSpan)
    {
        public override GraphNode GraphNode { get; } = GraphNode;
        
        public Component(
            ComponentProperty Property,
            GraphNode graphNode
        ) : this(Property, graphNode.State.TextSpan, graphNode)
        {
        }
    }
}