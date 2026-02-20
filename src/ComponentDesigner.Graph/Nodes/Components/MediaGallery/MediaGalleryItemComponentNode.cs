namespace ComponentDesigner.Nodes;

public sealed class MediaGalleryItemComponentNode : ComponentNode
{
    public override string Name => "media-gallery-item";

    public override IReadOnlyList<string> Aliases { get; } = ["media", "gallery-item", "item"];

    public override IReadOnlyList<ComponentProperty> Properties { get; }
    
    public ComponentProperty Media { get; }
    public ComponentProperty Description { get; }
    public ComponentProperty IsSpoiler { get; }

    public MediaGalleryItemComponentNode()
    {
        Properties =
        [
            Media = new("media", aliases: ["url"]),
            Description = new("description", isOptional: true),
            IsSpoiler = new("spoiler", isOptional: true, requiresValue: false)
        ];
    }

    public override Result<RenderedComponent> Emit(
        ComponentState state,
        ComponentEmitContext context,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    ) => ValidateAndRender(
        this,
        state,
        context,
        options,
        Validators.ValidateMediaGalleryItem,
        context.Renderer.RenderMediaGalleryItem,
        cancellationToken
    );
}