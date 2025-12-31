using System;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

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

    public override ImmutableArray<ComponentProperty> Properties { get; }

    protected override bool AllowChildrenInCX => true;

    public SelectMenuOptionComponentNode()
    {
        Properties =
        [
            Label = new(
                "label",
                renderer: CXValueGenerator.String,
                validators:
                [
                    Validators.StringRange(upper: Constants.STRING_SELECT_OPTION_LABEL_MAX_LENGTH)
                ]
            ),
            Value = new(
                "value",
                renderer: CXValueGenerator.String,
                validators:
                [
                    Validators.StringRange(upper: Constants.STRING_SELECT_OPTION_VALUE_MAX_LENGTH)
                ]
            ),
            Description = new(
                "description",
                isOptional: true,
                renderer: CXValueGenerator.String,
                validators:
                [
                    Validators.StringRange(upper: Constants.STRING_SELECT_OPTION_DESCRIPTION_MAX_LENGTH)
                ]
            ),
            Emoji = new(
                "emoji",
                isOptional: true,
                renderer: CXValueGenerator.Emoji
            ),
            Default = new(
                "default",
                isOptional: true,
                renderer: CXValueGenerator.Boolean,
                dotnetParameterName: "isDefault",
                requiresValue: false
            )
        ];
    }

    public override ComponentState? Create(
        ComponentStateInitializationContext context,
        IList<DiagnosticInfo> diagnostics
    )
    {
        var state = base.Create(context, diagnostics);

        if (
            context.CXNode is CXElement { Children.Count: 1 } element &&
            element.Children[0] is CXValue value
        )
        {
            state!.SubstitutePropertyValue(Label, value);
        }

        return state;
    }

    public override Result<string> Render(
        ComponentState state,
        IComponentContext context,
        ComponentRenderingOptions options
    ) => state
        .RenderProperties(this, context)
        .Map(x =>
            $"new {context.KnownTypes.SelectMenuOptionBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({
                x.WithNewlinePadding(4)
                    .PrefixIfSome(4)
                    .WrapIfSome(Environment.NewLine)
            })"
        );
}