using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public abstract record ComponentPropertyValueSource
{
    public static readonly ComponentPropertyValueSource None = new Unknown();

    public sealed record Attribute(CXAttribute AttributeSyntax) : ComponentPropertyValueSource;

    public sealed record Child(GraphNode Parent) : ComponentPropertyValueSource;

    public sealed record Unknown : ComponentPropertyValueSource;
}

public readonly record struct ComponentPropertyLocationInfo(
    CXTextSpan TextSpan,
    LexedCXTrivia LeadingTrivia,
    LexedCXTrivia TrailingTrivia
) : ISourceLocatable, IContainsTrivia
{
    public static ComponentPropertyLocationInfo From(ICXNode node)
        => new(node.TextSpan, node.LeadingTrivia, node.TrailingTrivia);

    public static implicit operator ComponentPropertyLocationInfo(CXTextSpan textSpan)
        => new(textSpan, LexedCXTrivia.Empty, LexedCXTrivia.Empty);

    public static implicit operator ComponentPropertyLocationInfo(CXToken token)
        => From(token);

    public static implicit operator ComponentPropertyLocationInfo(CXNode node)
        => From(node);
}

public abstract record ComponentPropertyValue(
    ComponentPropertyValueSource Source,
    ComponentProperty Property,
    ComponentPropertyLocationInfo Location
) : ISourceLocatable, IContainsTrivia
{
    public CXTextSpan TextSpan => Location.TextSpan;

    public LexedCXTrivia LeadingTrivia => Location.LeadingTrivia;
    public LexedCXTrivia TrailingTrivia => Location.TrailingTrivia;

    public string Name => Property.Name;

    public bool IsSourcedFromAttribute => Source is ComponentPropertyValueSource.Attribute;
    public bool IsSourcedFromParent => Source is ComponentPropertyValueSource.Child;

    public bool IsSome => !IsNone;
    public bool IsNone => this is None;
    public bool IsLiteral => this is Literal;
    public bool IsInterpolation => this is Interpolation;
    public bool IsComponent => this is Component;
    public bool IsMany => this is Many;

    public bool IsOne => !IsMany;

    public ComponentPropertyValue? AsSingle
    {
        get
        {
            if (this is not Many many) return this;

            if (many.Values.Count is not 1) return null;

            return many.Values[0];
        }
    }

    public IEnumerable<ComponentPropertyValue> AsFlattened
        => this is Many many ? [..many.Values.SelectMany(x => x.AsFlattened)] : [this];

    public ComponentPropertyValueKind Kind
        => _kind ??= this switch
        {
            None => ComponentPropertyValueKind.None,
            Literal => ComponentPropertyValueKind.Literal,
            Interpolation => ComponentPropertyValueKind.Interpolation,
            Component => ComponentPropertyValueKind.Component,
            Many { Values: { } values } =>
                ComponentPropertyValueKind.Many | values
                    .Aggregate(ComponentPropertyValueKind.None, (a, b) => a | b.Kind),
            _ => ComponentPropertyValueKind.None
        };

    public bool IsAttributeNameOnly => IsSourcedFromAttribute && IsNone;

    private ComponentPropertyValueKind? _kind;

    public bool IsValidBySpec => Matches(Property.Kind);

    public bool Matches(ComponentPropertyValueKind kind)
    {
        if (this is not Many many) return IsSimpleMatch(this, kind);

        return
            kind.HasFlag(ComponentPropertyValueKind.Many) &&
            many.Values.All(x => IsSimpleMatch(x, kind));


        static bool IsSimpleMatch(ComponentPropertyValue value, ComponentPropertyValueKind kind)
            => (
                value.IsNone ||
                (value.IsLiteral && kind.HasFlag(ComponentPropertyValueKind.Literal)) ||
                (value.IsInterpolation && kind.HasFlag(ComponentPropertyValueKind.Interpolation)) ||
                (value.IsComponent && kind.HasFlag(ComponentPropertyValueKind.Component))
            );
    }

    public sealed record None(
        ComponentPropertyValueSource Source,
        ComponentProperty Property,
        ComponentPropertyLocationInfo Location
    ) : ComponentPropertyValue(Source, Property, Location);

    public sealed record Literal(
        ComponentPropertyValueSource Source,
        ComponentProperty Property,
        ComponentPropertyLocationInfo Location,
        string Value
    ) : ComponentPropertyValue(Source, Property, Location);

    public sealed record Interpolation(
        ComponentPropertyValueSource Source,
        ComponentProperty Property,
        ComponentPropertyLocationInfo Location,
        IInterpolationInfo Info
    ) : ComponentPropertyValue(Source, Property, Location);

    public sealed record Component(
        ComponentPropertyValueSource Source,
        ComponentProperty Property,
        ComponentPropertyLocationInfo Location,
        GraphNode GraphNode
    ) : ComponentPropertyValue(Source, Property, Location)
    {
        public Component(
            ComponentPropertyValueSource Source,
            ComponentProperty Property,
            GraphNode GraphNode
        ) : this(Source, Property, GraphNode.TextSpan, GraphNode)
        {
        }
    }

    public sealed record Many(
        ComponentPropertyValueSource Source,
        ComponentProperty Property,
        ComponentPropertyLocationInfo Location,
        IReadOnlyList<ComponentPropertyValue> Values
    ) : ComponentPropertyValue(Source, Property, Location)
    {
        public Many(
            ComponentPropertyValueSource Source,
            ComponentProperty Property,
            IReadOnlyList<ComponentPropertyValue> Values
        ) : this(
            Source,
            Property,
            Values.Count is 0
                ? default
                : CXTextSpan.From(Values),
            Values
        )
        {
        }
    }
}