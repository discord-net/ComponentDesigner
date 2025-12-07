using System;
using System.Collections.Generic;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

public sealed class ThumbnailComponentNode : ComponentNode
{
    public override string Name => "thumbnail";

    public ComponentProperty Id { get; }
    public ComponentProperty Media { get; }
    public ComponentProperty Description { get; }
    public ComponentProperty Spoiler { get; }

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ThumbnailComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            Media = new(
                "media",
                aliases: ["href", "url"],
                renderer: Renderers.UnfurledMediaItem
            ),
            Description = new(
                "description",
                isOptional: true,
                renderer: Renderers.String,
                validators: [Validators.StringRange(upper: Constants.THUMBNAIL_DESCRIPTION_MAX_LENGTH)]
            ),
            Spoiler = new(
                "spoiler",
                isOptional: true,
                renderer: Renderers.Boolean,
                dotnetParameterName: "isSpoiler",
                requiresValue: false
            )
        ];
    }

    public override string Render(ComponentState state, IComponentContext context, ComponentRenderingOptions options)
        => $"""
            new {context.KnownTypes.ThumbnailBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({
                state.RenderProperties(this, context)
                    .PrefixIfSome(4)
                    .WithNewlinePadding(4)
                    .WrapIfSome(Environment.NewLine)
            })
            """;
}
