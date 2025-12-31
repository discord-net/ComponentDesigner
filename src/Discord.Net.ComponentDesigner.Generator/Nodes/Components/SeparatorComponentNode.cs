using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

public sealed class SeparatorComponentNode : ComponentNode
{
    public const string SEPARATOR_SPACING_QUALIFIED_NAME = "Discord.SeparatorSpacingSize";

    public override string Name => "separator";

    public ComponentProperty Id { get; }
    public ComponentProperty Divider { get; }
    public ComponentProperty Spacing { get; }

    public override ImmutableArray<ComponentProperty> Properties { get; }

    public SeparatorComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            Divider = new(
                "divider",
                isOptional: true,
                renderer: CXValueGenerator.Boolean,
                dotnetParameterName: "isDivider"
            ),
            Spacing = new(
                "spacing",
                aliases: ["size"],
                isOptional: true,
                renderer: CXValueGenerator.Enum(SEPARATOR_SPACING_QUALIFIED_NAME)
            )
        ];
    }

    public override Result<string> Render(
        ComponentState state,
        IComponentContext context,
        ComponentRenderingOptions options
    ) => state
        .RenderProperties(this, context)
        .Map(x =>
            $"""
             new {context.KnownTypes.SeparatorBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({
                 x.PrefixIfSome(4)
                     .WithNewlinePadding(4)
                     .WrapIfSome(Environment.NewLine)
             })
             """
        )
        .Map(state.ConformResult(ComponentBuilderKind.IMessageComponentBuilder, options.TypingContext));
}
