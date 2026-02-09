namespace ComponentDesigner.Nodes;

public sealed class MediaGalleryComponentNode : ComponentNode
{
    public override string Name => "media-gallery";

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ComponentProperty Id { get; }
    public ComponentProperty Items { get; }

    public MediaGalleryComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            Items = new("items")
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
        Validators.ValidateMediaGallery,
        context.Renderer.RenderMediaGallery,
        cancellationToken
    );
}