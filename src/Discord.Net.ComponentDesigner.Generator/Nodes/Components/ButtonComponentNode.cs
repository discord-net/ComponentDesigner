using System;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

public sealed class ButtonComponentState : ComponentState
{
    public ButtonKind InferredKind { get; set; } = ButtonKind.Default;
}

public enum ButtonKind
{
    Default,
    Link,
    Premium
}

public sealed class ButtonComponentNode : ComponentNode<ButtonComponentState>
{
    public const string BUTTON_STYLE_ENUM = "Discord.ButtonStyle";
    public const int BUTTON_STYLE_LINK_VALUE = 5;
    public const int BUTTON_STYLE_PREMIUM_VALUE = 6;

    public override string Name => "button";

    public override IReadOnlyList<ComponentProperty> Properties { get; }

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
                renderer: Renderers.RenderEnum(BUTTON_STYLE_ENUM)
            ),
            Label = new ComponentProperty(
                "label",
                isOptional: true,
                validators: [Validators.StringRange(upper: Constants.BUTTON_MAX_LABEL_LENGTH)],
                renderer: Renderers.String
            ),
            Emoji = new ComponentProperty(
                "emoji",
                isOptional: true,
                aliases: ["emote"],
                renderer: Renderers.Emoji,
                dotnetParameterName: "emote"
            ),
            CustomId = new(
                "customId",
                isOptional: true,
                validators: [Validators.StringRange(upper: Constants.CUSTOM_ID_MAX_LENGTH)],
                renderer: Renderers.String
            ),
            SkuId = new(
                "skuId",
                aliases: ["sku"],
                isOptional: true,
                validators: [Validators.Snowflake],
                renderer: Renderers.Snowflake
            ),
            Url = new(
                "url",
                isOptional: true,
                validators: [Validators.StringRange(upper: Constants.BUTTON_URL_MAX_LENGTH)],
                renderer: Renderers.String
            ),
            Disabled = new(
                "disabled",
                isOptional: true,
                renderer: Renderers.Boolean,
                dotnetParameterName: "isDisabled"
            )
        ];
    }

    public override void AddGraphNode(ComponentGraphInitializationContext context)
    {
        /*
         * Auto row semantics:
         * We only want to insert rows when:
         *   - Auto rows is enabled
         *   - A non-row parent exists
         *   - A left-hand sibling is not a part of an auto row
         */
        if (
            !context.Options.EnableAutoRows ||
            context.ParentGraphNode is null ||
            context.ParentGraphNode.Inner is ActionRowComponentNode
        )
        {
            base.AddGraphNode(context);
            return;
        }

        // check the adjacent sibling
        var sibling = context.ParentGraphNode.Children.LastOrDefault();

        if (sibling is not null && CanAddToAutoRow(sibling))
        {
            // add this node to the auto row
            context.Push(this, parent: sibling);
            return;
        }
        
        // we can create an auto row
        context.Push(AutoActionRowComponentNode.Instance, children: [(CXNode)context.CXNode]);

        /*
         * we can add to an adjacent auto row if:
         *  - the adjacent node is an auto row
         *  - the auto row has less than 5 children
         *  - the children of the auto row is either buttons or dynamic nodes
         */
        static bool CanAddToAutoRow(CXGraph.Node node)
            => node is { Inner: AutoActionRowComponentNode, Children.Count: < 5 } &&
               node
                   .Children
                   .All(x => x.Inner is ButtonComponentNode or IDynamicComponentNode);
    }

    public override ButtonComponentState? CreateState(ComponentStateInitializationContext context)
    {
        if (context.Node is not CXElement element) return null;

        var state = new ButtonComponentState()
        {
            Source = element
        };

        if (element.Children.Count > 0 && element.Children[0] is CXValue value)
            state.SubstitutePropertyValue(Label, value);

        InferButtonKind(state);

        return state;
    }

    public override void UpdateState(ref ButtonComponentState state, IComponentContext context)
    {
        state.InferredKind = FurtherInferredButtonKindWithContext(state, context);
    }

    private void InferButtonKind(ButtonComponentState state)
    {
        if (
            state.GetProperty(Url).IsSpecified &&
            !state.GetProperty(CustomId).IsSpecified &&
            !state.GetProperty(SkuId).IsSpecified
        )
        {
            state.InferredKind = ButtonKind.Link;
        }
        else if (
            !state.GetProperty(Url).IsSpecified &&
            !state.GetProperty(CustomId).IsSpecified &&
            state.GetProperty(SkuId).IsSpecified
        )
        {
            state.InferredKind = ButtonKind.Premium;
        }
        else if (state.GetProperty(Style).TryGetLiteralValue(out var style))
        {
            state.InferredKind = style.ToLowerInvariant() switch
            {
                "link" => ButtonKind.Link,
                "premium" => ButtonKind.Premium,
                _ => ButtonKind.Default
            };
        }
    }

    private ButtonKind FurtherInferredButtonKindWithContext(ButtonComponentState state, IComponentContext context)
    {
        if (state.InferredKind is not ButtonKind.Default) return state.InferredKind;

        // check for interpolated constants
        var style = state.GetProperty(Style);

        switch (style.Value)
        {
            case CXValue.Multipart multipart when Renderers.IsLoneInterpolatedLiteral(context, multipart, out var info):
                return FromInterpolation(info);
            case CXValue.Interpolation interpolation:
                return FromInterpolation(context.GetInterpolationInfo(interpolation));
        }

        return state.InferredKind;

        ButtonKind FromInterpolation(DesignerInterpolationInfo info)
        {
            var constant = info.Constant;

            if (!constant.HasValue) return state.InferredKind;

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

            return state.InferredKind;
        }
    }

    public override void Validate(ButtonComponentState state, IComponentContext context)
    {
        var label = state.GetProperty(Label);

        if (
            label.Attribute?.Value is not null &&
            label.Value is not null &&
            label.Value != label.Attribute.Value
        )
        {
            context.AddDiagnostic(
                Diagnostic.Create(
                    Diagnostics.ButtonLabelDuplicate,
                    context.GetLocation(label.Value!)
                )
            );
        }

        switch (state.InferredKind)
        {
            case ButtonKind.Link:
                // url is required
                state.GetProperty(Url).ReportPropertyConfigurationDiagnostics(
                    context,
                    state,
                    optional: false,
                    requiresValue: true
                );

                state.ReportPropertyNotAllowed(CustomId, context);
                state.ReportPropertyNotAllowed(SkuId, context);

                state.RequireOneOf(context, Label, Emoji);
                break;
            case ButtonKind.Premium:
                // url is required
                state.GetProperty(SkuId).ReportPropertyConfigurationDiagnostics(
                    context,
                    state,
                    optional: false,
                    requiresValue: true
                );

                state.ReportPropertyNotAllowed(CustomId, context);
                state.ReportPropertyNotAllowed(Url, context);
                state.ReportPropertyNotAllowed(Label, context);
                state.ReportPropertyNotAllowed(Emoji, context);
                break;
            case ButtonKind.Default:
                // custom id is required
                state.GetProperty(CustomId).ReportPropertyConfigurationDiagnostics(
                    context,
                    state,
                    optional: false,
                    requiresValue: true
                );

                state.ReportPropertyNotAllowed(SkuId, context);
                state.ReportPropertyNotAllowed(Url, context);
                state.RequireOneOf(context, Label, Emoji);
                break;
        }

        base.Validate(state, context);
    }

    public override string Render(ButtonComponentState state, IComponentContext context,
        ComponentRenderingOptions options)
    {
        string style;

        if (state.InferredKind is not ButtonKind.Default)
            style = $"global::{BUTTON_STYLE_ENUM}.{state.InferredKind}";
        else
        {
            // use the provided property from state
            var stylePropertyValue = state.GetProperty(Style);

            style = stylePropertyValue.Value is null
                ? $"global::{BUTTON_STYLE_ENUM}.Primary"
                : Style.Renderer(context, stylePropertyValue);
        }

        var props = string.Join(
            $",{Environment.NewLine}",
            [
                $"style: {style}",
                state.RenderProperties(
                    this,
                    context,
                    ignorePredicate: x => x == Style
                )
            ]
        );

        return $"""
                new {context.KnownTypes.ButtonBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({
                    props
                        .WithNewlinePadding(4)
                        .PrefixIfSome(4)
                        .WrapIfSome(Environment.NewLine)
                })
                """;
    }
}