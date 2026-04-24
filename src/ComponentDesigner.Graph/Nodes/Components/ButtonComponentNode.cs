using System.Collections.Immutable;
using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public enum ButtonKind
{
    Default,
    Link,
    Premium
}

public sealed record ButtonState : ComponentState
{
    public new CXElement CXNode { get; init; }

    public ButtonKind? InferredKind { get; init; }

    public ButtonState(
        CXElement element,
        ComponentNodeInitializationContext context,
        CancellationToken cancellationToken
    ) : base(context.GraphNode, element, context, cancellationToken)
    {
        CXNode = element;
    }
}

public sealed class ButtonComponentNode : ComponentNode<ButtonState>
{
    public static readonly IReadOnlyList<string> ValidButtonStyles =
    [
        "primary",
        "secondary",
        "success",
        "danger",
        "link",
        "premium"
    ];

    public const int BUTTON_STYLE_LINK_VALUE = 5;
    public const int BUTTON_STYLE_PREMIUM_VALUE = 6;

    public override string Name => "button";

    public override IReadOnlyList<string> Aliases { get; } =
    [
        "link-button",
        "premium-button"
    ];

    public override ComponentTargetType Target => ComponentTargetType.Message;

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ComponentProperty Id { get; }
    public ComponentProperty Style { get; }
    public ComponentProperty Label { get; }
    public ComponentProperty Emoji { get; }
    public ComponentProperty CustomId { get; }
    public ComponentProperty SkuId { get; }
    public ComponentProperty Url { get; }
    public ComponentProperty Disabled { get; }

    public ButtonComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            Style = new ComponentProperty(
                "style",
                isOptional: true,
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            Label = new ComponentProperty(
                "label",
                isOptional: true,
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            Emoji = new ComponentProperty(
                "emoji",
                isOptional: true,
                aliases: ["emote"],
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            CustomId = new(
                "customId",
                isOptional: true,
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            SkuId = new(
                "skuId",
                aliases: ["sku"],
                isOptional: true,
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            Url = new(
                "url",
                isOptional: true,
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            Disabled = new(
                "disabled",
                isOptional: true,
                requiresValue: false,
                kind: ComponentPropertyValueKind.SyntaxValue
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

    public override ButtonState? CreateState(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    )
    {
        if (context.CXNode is not CXElement element) return null;

        var state = new ButtonState(element, context, cancellationToken);

        // label can be ingested from children
        foreach (var childSyntax in element.Children)
        {
            if (childSyntax is CXValue valueSyntax)
            {
                if (state.GetPropertyValue(Label).IsSome)
                {
                    diagnostics.Add(
                        Diagnostic
                            .DuplicatePropertyValue(Label)
                            .At(valueSyntax)
                    );
                }
                else
                {
                    state.SetPropertyValue(context, Label, valueSyntax, cancellationToken);
                }

                continue;
            }

            diagnostics.Add(
                Diagnostic
                    .InvalidChildOfComponent(this, childSyntax)
                    .At(childSyntax)
            );
        }

        var inferredKind = InferButtonKindFromUsage(element, state);

        if (inferredKind is null) return state;

        if (state.GetPropertyValue(Style).IsNone)
            state.SetPropertyValue(
                Style,
                new ComponentPropertyValue.Literal(
                    ComponentPropertyValueSource.Synthetic.Instance,
                    Style,
                    state.TextSpan,
                    inferredKind switch
                    {
                        ButtonKind.Default => "primary",
                        ButtonKind.Link => "link",
                        ButtonKind.Premium => "premium",
                        _ => throw new NotImplementedException($"No case for {inferredKind}")
                    }
                )
            );

        return state with
        {
            InferredKind = InferButtonKindFromUsage(element, state)
        };
    }

    private ButtonKind? InferButtonKindFromUsage(
        CXElement element,
        ButtonState state
    )
    {
        if (element.Identifier is "premium-button") return ButtonKind.Premium;
        if (element.Identifier is "link-button") return ButtonKind.Link;

        if (
            state.GetPropertyValue(Url).IsSome &&
            state.GetPropertyValue(CustomId).IsNone &&
            state.GetPropertyValue(SkuId).IsNone
        )
        {
            return ButtonKind.Link;
        }

        if (
            state.GetPropertyValue(Url).IsNone &&
            state.GetPropertyValue(CustomId).IsNone &&
            state.GetPropertyValue(SkuId).IsSome
        )
        {
            return ButtonKind.Premium;
        }

        var styleProperty = state.GetPropertyValue(Style);

        switch (styleProperty.AsSingle)
        {
            case ComponentPropertyValue.Literal { Value: var literal }:
                switch (literal.ToLowerInvariant())
                {
                    case "link": return ButtonKind.Link;
                    case "premium": return ButtonKind.Premium;
                    case var invalid when !ValidButtonStyles.Contains(invalid.ToLowerInvariant()):
                        return null;
                }

                break;

            case ComponentPropertyValue.Interpolation { Info: var info }:
                return FromInterpolation(info);
        }

        return ButtonKind.Default;

        ButtonKind FromInterpolation(IInterpolationInfo info)
        {
            var constant = info.ConstantValue;

            if (!constant.IsSpecified) return ButtonKind.Default;

            switch (constant.Value)
            {
                case string str:
                    switch (str.ToLowerInvariant())
                    {
                        case "link": return ButtonKind.Link;
                        case "premium": return ButtonKind.Premium;
                    }

                    break;
                case int i:
                    switch (i)
                    {
                        case BUTTON_STYLE_LINK_VALUE: return ButtonKind.Link;
                        case BUTTON_STYLE_PREMIUM_VALUE: return ButtonKind.Premium;
                    }

                    break;
            }

            return ButtonKind.Default;
        }
    }

    public override void Validate(
        IComponentContext context, ButtonState state, IDiagnosticBag bag,
        CancellationToken cancellationToken = default
    ) => Validators.ValidateButton(context, this, state, bag);
}