namespace ComponentDesigner.Nodes;

public sealed class ThumbnailComponentNode : ComponentNode
{
    public override string Name => "thumbnail";

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ComponentProperty Id { get; }
    public ComponentProperty Media { get; }
    public ComponentProperty Description { get; }
    public ComponentProperty IsSpoiler { get; }

    public ThumbnailComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            Media = new(
                "media",
                aliases: ["href", "url"],
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
    ) => Validators.ValidateThumbnail(context, this, state, bag);

    public override Result<RenderedComponent> Render(
        ComponentEmitContext context,
        ComponentState state,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    ) => context.Renderer
        .RenderThumbnail(context, this, state, options.TypingContext, cancellationToken);
}