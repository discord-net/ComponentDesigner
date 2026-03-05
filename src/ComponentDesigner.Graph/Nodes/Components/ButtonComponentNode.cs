using System.Collections.Immutable;
using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public enum ButtonKind
{
    Default,
    Link,
    Premium
}

public sealed record ButtonState(
    GraphNode GraphNode,
    CXElement CXNode,
    ButtonKind? InferredKind = null
) : ComponentState(GraphNode, CXNode)
{
    public new CXElement CXNode { get; init; } = CXNode;
}

public sealed class ButtonComponentNode : ComponentNode<ButtonState>
{
    public static readonly ImmutableArray<string> ValidButtonStyles =
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

    // label can be in children
    public override bool AllowChildrenInCX => true;

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
                isOptional: true
            ),
            Label = new ComponentProperty(
                "label",
                isOptional: true
            ),
            Emoji = new ComponentProperty(
                "emoji",
                isOptional: true,
                aliases: ["emote"]
            ),
            CustomId = new(
                "customId",
                isOptional: true
            ),
            SkuId = new(
                "skuId",
                aliases: ["sku"],
                isOptional: true
            ),
            Url = new(
                "url",
                isOptional: true
            ),
            Disabled = new(
                "disabled",
                isOptional: true
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

    public override ButtonState? Initialize(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    )
    {
        if (context.CXNode is not CXElement element) return null;

        var state = new ButtonState(context.GraphNode, element);

        // label can be ingested from children
        state.IngestChildrenAsScalarValueForProperty(Label);
        
        return state with
        {
            InferredKind = InferButtonKindFromUsage(context.GraphContext, element, state, diagnostics)
        };
    }

    private ButtonKind? InferButtonKindFromUsage(
        IComponentContext context,
        CXElement element,
        ButtonState state,
        IDiagnosticBag diagnostics
    )
    {
        if (element.Identifier is "premium-button") return ButtonKind.Premium;
        if (element.Identifier is "link-button") return ButtonKind.Link;

        if (
            state.GetPropertyValue(Url).IsSpecified &&
            !state.GetPropertyValue(CustomId).IsSpecified &&
            !state.GetPropertyValue(SkuId).IsSpecified
        )
        {
            return ButtonKind.Link;
        }

        if (
            !state.GetPropertyValue(Url).IsSpecified &&
            !state.GetPropertyValue(CustomId).IsSpecified &&
            state.GetPropertyValue(SkuId).IsSpecified
        )
        {
            return ButtonKind.Premium;
        }

        var styleProperty = state.GetPropertyValue(Style);

        switch (styleProperty.CXValue)
        {
            case CXValue.Multipart multipart
                when multipart.TryGetSingleInterpolation(context, out var info):
                return FromInterpolation(info);
            case CXValue.Interpolation interpolation:
                return FromInterpolation(context.GetInterpolationInfo(interpolation));
            case not null when styleProperty.TryGetLiteralValue(out var literal):
                switch (literal.ToLowerInvariant())
                {
                    case "link": return ButtonKind.Link;
                    case "premium": return ButtonKind.Premium;
                    case var invalid when !ValidButtonStyles.Contains(invalid.ToLowerInvariant()):
                        return null;
                }

                break;
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

    public override Result<RenderedComponent> Render(
        ComponentEmitContext context,
        ButtonState state,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    ) => context.Renderer.RenderButton(context, this, state, options.TypingContext, cancellationToken);
}