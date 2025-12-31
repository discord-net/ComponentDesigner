using System;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Discord.CX.Util;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

public sealed record ButtonComponentState(
    GraphNode GraphNode,
    ICXNode Source,
    ButtonKind? InferredKind = null
) : ComponentState(GraphNode, Source)
{
    public bool Equals(ButtonComponentState? other)
    {
        if (other is null) return false;

        return InferredKind == other.InferredKind && base.Equals(other);
    }


    public override int GetHashCode()
        => Hash.Combine(InferredKind, base.GetHashCode());
}

public enum ButtonKind
{
    Default,
    Link,
    Premium
}

public sealed class ButtonComponentNode : ComponentNode<ButtonComponentState>
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

    public const string BUTTON_STYLE_ENUM = "Discord.ButtonStyle";
    public const int BUTTON_STYLE_LINK_VALUE = 5;
    public const int BUTTON_STYLE_PREMIUM_VALUE = 6;

    public override string Name => "button";

    public override ImmutableArray<ComponentProperty> Properties { get; }

    public ComponentProperty Id { get; }
    public ComponentProperty Style { get; }
    public ComponentProperty Label { get; }
    public ComponentProperty Emoji { get; }
    public ComponentProperty CustomId { get; }
    public ComponentProperty SkuId { get; }
    public ComponentProperty Url { get; }
    public ComponentProperty Disabled { get; }

    protected override bool AllowChildrenInCX => true;

    public ButtonComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            Style = new ComponentProperty(
                "style",
                isOptional: true,
                renderer: CXValueGenerator.Enum(BUTTON_STYLE_ENUM)
            ),
            Label = new ComponentProperty(
                "label",
                isOptional: true,
                validators: [Validators.StringRange(upper: Constants.BUTTON_MAX_LABEL_LENGTH)],
                renderer: CXValueGenerator.String
            ),
            Emoji = new ComponentProperty(
                "emoji",
                isOptional: true,
                aliases: ["emote"],
                renderer: CXValueGenerator.Emoji,
                dotnetParameterName: "emote"
            ),
            CustomId = new(
                "customId",
                isOptional: true,
                validators: [Validators.StringRange(upper: Constants.CUSTOM_ID_MAX_LENGTH)],
                renderer: CXValueGenerator.String
            ),
            SkuId = new(
                "skuId",
                aliases: ["sku"],
                isOptional: true,
                renderer: CXValueGenerator.Snowflake
            ),
            Url = new(
                "url",
                isOptional: true,
                validators: [Validators.StringRange(upper: Constants.BUTTON_URL_MAX_LENGTH)],
                renderer: CXValueGenerator.String
            ),
            Disabled = new(
                "disabled",
                isOptional: true,
                renderer: CXValueGenerator.Boolean,
                dotnetParameterName: "isDisabled"
            )
        ];
    }

    public override void AddGraphNode(ComponentGraphInitializationContext context)
    {
        if (!AutoActionRowComponentNode.AddPossibleAutoRowNode(this, context))
            base.AddGraphNode(context);
    }

    public override ButtonComponentState? CreateState(
        ComponentStateInitializationContext context,
        IList<DiagnosticInfo> diagnostics
    )
    {
        if (context.CXNode is not CXElement element) return null;

        var state = new ButtonComponentState(
            context.GraphNode,
            context.CXNode
        );

        if (element.Children.Count > 0 && element.Children[0] is CXValue value)
            state.SubstitutePropertyValue(Label, value);

        return state with
        {
            InferredKind = InferButtonKind(context.GraphContext, state, diagnostics)
        };
    }


    private ButtonKind? InferButtonKind(
        IComponentContext context,
        ButtonComponentState state,
        IList<DiagnosticInfo> diagnostics
    )
    {
        if (
            state.GetProperty(Url).IsSpecified &&
            !state.GetProperty(CustomId).IsSpecified &&
            !state.GetProperty(SkuId).IsSpecified
        )
        {
            return ButtonKind.Link;
        }

        if (
            !state.GetProperty(Url).IsSpecified &&
            !state.GetProperty(CustomId).IsSpecified &&
            state.GetProperty(SkuId).IsSpecified
        )
        {
            return ButtonKind.Premium;
        }

        var styleProperty = state.GetProperty(Style);
        switch (styleProperty.Value)
        {
            case CXValue.Multipart multipart
                when multipart.IsLoneInterpolatedLiteral(context, out var info):
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

        ButtonKind FromInterpolation(DesignerInterpolationInfo info)
        {
            var constant = info.Constant;

            if (!constant.HasValue) return ButtonKind.Default;

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
        ButtonComponentState state,
        IComponentContext context,
        IList<DiagnosticInfo> diagnostics
    )
    {
        var label = state.GetProperty(Label);

        if (
            label.Attribute?.Value is not null &&
            label.Value is not null &&
            !label.Value.Equals(label.Attribute.Value)
        )
        {
            diagnostics.Add(
                Diagnostics.ButtonLabelDuplicate,
                label.Value
            );
        }

        switch (state.InferredKind)
        {
            case ButtonKind.Link:
                // url is required
                state.GetProperty(Url).ReportPropertyConfigurationDiagnostics(
                    context,
                    state,
                    diagnostics,
                    optional: false,
                    requiresValue: true
                );

                state.ReportPropertyNotAllowed(CustomId, context, diagnostics);
                state.ReportPropertyNotAllowed(SkuId, context, diagnostics);

                state.RequireOneOf(context, diagnostics, Label, Emoji);
                break;
            case ButtonKind.Premium:
                // url is required
                state.GetProperty(SkuId).ReportPropertyConfigurationDiagnostics(
                    context,
                    state,
                    diagnostics,
                    optional: false,
                    requiresValue: true
                );

                state.ReportPropertyNotAllowed(CustomId, context, diagnostics);
                state.ReportPropertyNotAllowed(Url, context, diagnostics);
                state.ReportPropertyNotAllowed(Label, context, diagnostics);
                state.ReportPropertyNotAllowed(Emoji, context, diagnostics);
                break;
            case ButtonKind.Default:
                // custom id is required
                state.GetProperty(CustomId).ReportPropertyConfigurationDiagnostics(
                    context,
                    state,
                    diagnostics,
                    optional: false,
                    requiresValue: true
                );

                state.ReportPropertyNotAllowed(SkuId, context, diagnostics);
                state.ReportPropertyNotAllowed(Url, context, diagnostics);
                state.RequireOneOf(context, diagnostics, Label, Emoji);
                break;
        }

        base.Validate(state, context, diagnostics);
    }

    public override Result<string> Render(
        ButtonComponentState state,
        IComponentContext context,
        ComponentRenderingOptions options
    )
    {
        Result<string> style;

        if (state.InferredKind is not null and not ButtonKind.Default)
            style = $"global::{BUTTON_STYLE_ENUM}.{state.InferredKind}";
        else
        {
            // use the provided property from state
            var stylePropertyValue = state.GetProperty(Style);

            style = stylePropertyValue.Value is null
                ? $"global::{BUTTON_STYLE_ENUM}.Primary"
                : Style.Renderer(
                    context,
                    new CXValueGeneratorTarget.ComponentProperty(stylePropertyValue),
                    CXValueGeneratorOptions.Default
                );
        }

        return style
            .Map(x => $"style: {x}")
            .Combine(state.RenderProperties(this, context, ignorePredicate: x => x == Style))
            .Map(x =>
                $"new {context.KnownTypes.ButtonBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({
                    string.Join($",{Environment.NewLine}", [x.Left, x.Right])
                        .WithNewlinePadding(4)
                        .PrefixIfSome(4)
                        .WrapIfSome(Environment.NewLine)
                })"
            )
            .Map(state.ConformResult(ComponentBuilderKind.IMessageComponentBuilder, options.TypingContext));
    }
}