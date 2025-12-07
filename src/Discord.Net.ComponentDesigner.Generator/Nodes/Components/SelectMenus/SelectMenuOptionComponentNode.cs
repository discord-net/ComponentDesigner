using System;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Discord.CX.Nodes.Components.SelectMenus;

public sealed class SelectMenuOptionComponentNode : ComponentNode
{
    public override string Name => "select-menu-option";

    public override IReadOnlyList<string> Aliases { get; } = ["option"];

    public ComponentProperty Label { get; }
    public ComponentProperty Value { get; }
    public ComponentProperty Description { get; }
    public ComponentProperty Emoji { get; }
    public ComponentProperty Default { get; }

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    protected override bool AllowChildrenInCX => true;

    public SelectMenuOptionComponentNode()
    {
        Properties =
        [
            Label = new(
                "label",
                renderer: Renderers.String,
                validators:
                [
                    Validators.StringRange(upper: Constants.STRING_SELECT_OPTION_LABEL_MAX_LENGTH)
                ]
            ),
            Value = new(
                "value",
                renderer: Renderers.String,
                validators:
                [
                    Validators.StringRange(upper: Constants.STRING_SELECT_OPTION_VALUE_MAX_LENGTH)
                ]
            ),
            Description = new(
                "description",
                isOptional: true,
                renderer: Renderers.String,
                validators:
                [
                    Validators.StringRange(upper: Constants.STRING_SELECT_OPTION_DESCRIPTION_MAX_LENGTH)
                ]
            ),
            Emoji = new(
                "emoji",
                isOptional: true,
                renderer: Renderers.Emoji
            ),
            Default = new(
                "default",
                isOptional: true,
                renderer: Renderers.Boolean,
                dotnetParameterName: "isDefault",
                requiresValue: false
            )
        ];
    }

    public override ComponentState? Create(ComponentStateInitializationContext context)
    {
        var state = base.Create(context);

        if (
            context.Node is CXElement { Children.Count: 1 } element &&
            element.Children[0] is CXValue value
        )
        {
            state!.SubstitutePropertyValue(Label, value);
        }

        return state;
    }

    public override void Validate(ComponentState state, IComponentContext context)
    {
        base.Validate(state, context);
    }

    public override string Render(ComponentState state, IComponentContext context, ComponentRenderingOptions options)
        => $"""
            new {context.KnownTypes.SelectMenuOptionBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({
                state.RenderProperties(this, context)
                    .WithNewlinePadding(4)
                    .PrefixIfSome(4)
                    .WrapIfSome(Environment.NewLine)
            })
            """;
}