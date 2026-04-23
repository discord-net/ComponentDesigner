using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public enum SelectMenuKind
{
    Unknown,

    String,
    User,
    Role,
    Channel,
    Mentionable
}

public sealed record SelectMenuState : ComponentState
{
    public SelectMenuKind Kind { get; init; }
    public new CXElement CXNode { get; init; }

    public SelectMenuState(
        SelectMenuKind kind,
        CXElement element,
        ComponentNodeInitializationContext context,
        CancellationToken cancellationToken
    ) : base(context, cancellationToken)
    {
        CXNode = element;
        Kind = kind;
    }
}

public sealed class SelectMenuComponentNode : ComponentNode<SelectMenuState>
{
    public override string Name => "select-menu";

    public override IReadOnlyList<string> Aliases { get; } =
    [
        "select",
        "string-select",
        "string-select-menu",
        "string-menu",
        "user-select",
        "user-select-menu",
        "user-menu",
        "role-select",
        "role-select-menu",
        "role-menu",
        "channel-select",
        "channel-select-menu",
        "channel-menu",
        "mention-select",
        "mention-select-menu",
        "mentionable-select",
        "mentionable-select-menu",
        "menu",
    ];

    public override ComponentTargetType Target => ComponentTargetType.Any;
    
    public ComponentProperty Id { get; }
    public ComponentProperty Type { get; }
    public ComponentProperty CustomId { get; }
    public ComponentProperty ChannelTypes { get; }
    public ComponentProperty Placeholder { get; }
    public ComponentProperty MinValues { get; }
    public ComponentProperty MaxValues { get; }
    public ComponentProperty Required { get; }
    public ComponentProperty Disabled { get; }
    public ComponentProperty Options { get; }
    public ComponentProperty DefaultValues { get; }

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public SelectMenuComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            Type = new(
                "type",
                isOptional: true,
                isSynthetic: true,
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            CustomId = new(
                "customId",
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            ChannelTypes = new(
                "channelTypes",
                isOptional: true,
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            Placeholder = new(
                "placeholder",
                isOptional: true,
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            MinValues = new(
                "minValues",
                aliases: ["min"],
                isOptional: true,
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            MaxValues = new(
                "maxValues",
                aliases: ["max"],
                isOptional: true,
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            Required = new(
                "required",
                isOptional: true,
                requiresValue: false,
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            Disabled = new(
                "disabled",
                isOptional: true,
                requiresValue: false,
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            Options = new(
                "options",
                isOptional: true,
                kind: ComponentPropertyValueKind.Any
            ),
            DefaultValues = new(
                "defaultValues",
                isOptional: true,
                kind: ComponentPropertyValueKind.Any
            )
        ];
    }

    public override void RegisterGraphNode(
        ComponentGraphInitializationContext context,
        CancellationToken cancellationToken = default
    )
    {
        if (!AutoActionRowComponentNode.TryInsertActionRow(this, context))
            base.RegisterGraphNode(context, includeElementChildren: false, cancellationToken);
    }

    public override SelectMenuState? CreateState(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    )
    {
        if (context.CXNode is not CXElement element) return null;

        InferKind(context.GraphContext, element).TryUnwrap(diagnostics, out var kind);

        var state = new SelectMenuState(
            kind,
            element,
            context,
            cancellationToken
        );

        if (kind is SelectMenuKind.Unknown) return state;

        using var _ = List<ComponentPropertyValue>.Pooled(out var childValues);
        childValues.Clear();

        var childProperty = kind is SelectMenuKind.String
            ? Options
            : DefaultValues;

        foreach (var childSyntax in element.Children)
        {
            switch (childSyntax)
            {
                case CXElement childElement:
                    childValues.AddRange(
                        context
                            .PushAsChildren(childElement, cancellationToken)
                            .Select(x => new ComponentPropertyValue.Component(state.ChildSource, childProperty, x))
                    );
                    break;

                case CXValue value:
                    childValues.AddRange(
                        state
                            .BuildPropertyValueFromSyntax(
                                context,
                                childProperty,
                                state.ChildSource,
                                value,
                                value.TextSpan,
                                cancellationToken
                            )
                            .AsFlattened
                    );
                    break;
                default:
                    diagnostics.Add(
                        Diagnostic.InvalidChildOfComponent(this, childSyntax).At(childSyntax)
                    );
                    break;
            }
        }

        if (childValues.Count is 1)
        {
            state.SetPropertyValue(childProperty, childValues[0]);
        }
        else if (childValues.Count > 1)
        {
            state.SetPropertyValue(
                childProperty,
                new ComponentPropertyValue.Many(
                    state.ChildSource,
                    childProperty,
                    [..childValues]
                )
            );
        }

        return state;
    }

    private Result<SelectMenuKind> InferKind(
        IComponentContext context,
        CXElement element
    )
    {
        if (element.Identifier.StartsWith("user", StringComparison.InvariantCultureIgnoreCase))
            return SelectMenuKind.User;

        if (element.Identifier.StartsWith("role", StringComparison.InvariantCultureIgnoreCase))
            return SelectMenuKind.Role;

        if (element.Identifier.StartsWith("channel", StringComparison.InvariantCultureIgnoreCase))
            return SelectMenuKind.Channel;

        if (element.Identifier.StartsWith("mention", StringComparison.InvariantCultureIgnoreCase))
            return SelectMenuKind.Mentionable;

        if (element.Identifier.StartsWith("string", StringComparison.InvariantCultureIgnoreCase))
            return SelectMenuKind.String;

        var typeAttribute = element
            .Attributes
            .FirstOrDefault(x => x.Identifier.Equals("type", StringComparison.InvariantCultureIgnoreCase));

        if (typeAttribute is null)
        {
            return element.IdentifierTextSpanOrElementTextSpan.Report(
                Diagnostic.TypelessSelectMenu
            );
        }

        if (typeAttribute.Value is null)
        {
            return typeAttribute.Report(
                Diagnostic.RequiredPropertyNotSpecified(this, Type)
            );
        }

        if (typeAttribute.Value is not CXValue.StringLiteral literal)
        {
            return typeAttribute.Value.Report(
                Diagnostic.ExpectedAConstantValue
            );
        }

        if (!Enum.TryParse<SelectMenuKind>(literal.Tokens.ToValueString(), ignoreCase: true, out var kind))
        {
            return typeAttribute.Value.Report(
                Diagnostic.NotAValidEnumVariant(
                    "SelectMenuKind",
                    literal.Tokens.ToValueString()
                )
            );
        }

        return kind;
    }

    public override void Validate(
        IComponentContext context, SelectMenuState state, IDiagnosticBag bag,
        CancellationToken cancellationToken = default
    ) => Validators.ValidateSelectMenu(context, this, state, bag, cancellationToken);
}