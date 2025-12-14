using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Discord.CX.Nodes.Components;

public sealed class MediaGalleryItemComponentNode : ComponentNode
{
    public override string Name => "media-gallery-item";

    public override IReadOnlyList<string> Aliases { get; } = ["gallery-item", "media", "item"];

    public ComponentProperty Url { get; }
    public ComponentProperty Description { get; }
    public ComponentProperty Spoiler { get; }

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public MediaGalleryItemComponentNode()
    {
        Properties =
        [
            Url = new(
                "url",
                aliases: ["media"],
                renderer: Renderers.UnfurledMediaItem,
                dotnetParameterName: "media"
            ),
            Description = new(
                "description",
                isOptional: true,
                renderer: Renderers.String
            ),
            Spoiler = new(
                "spoiler",
                isOptional: true,
                renderer: Renderers.Boolean,
                dotnetParameterName: "isSpoiler"
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
             new {context.KnownTypes.MediaGalleryItemPropertiesType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}(
                 {x.WithNewlinePadding(4)}
             )
             """
        );
}