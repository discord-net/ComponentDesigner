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
            Media = new(
                "media",
                aliases: ["url"],
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            Description = new(
                "description",
                isOptional: true,
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            IsSpoiler = new(
                "spoiler",
                isOptional: true,
                requiresValue: false,
                kind: ComponentPropertyValueKind.SyntaxValue
            )
        ];
    }

    public override void Validate(
        IComponentContext context, ComponentState state, IDiagnosticBag bag,
        CancellationToken cancellationToken = default
    ) => Validators.ValidateMediaGalleryItem(context, this, state, bag);
}