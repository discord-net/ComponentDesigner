namespace ComponentDesigner.Nodes;

public sealed class FileComponentNode : ComponentNode
{
    public override string Name => "file";

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ComponentProperty Id { get; }
    public ComponentProperty Media { get; }
    public ComponentProperty IsSpoiler { get; }

    public FileComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            Media = new(
                "media",
                aliases: ["url"],
                autoFillMode: PropertyAutoFillMode.String
            ),
            IsSpoiler = new(
                "spoiler",
                isOptional: true,
                requiresValue: false,
                autoFillMode: PropertyAutoFillMode.String,
                autoFillChoices: ["true", "false"]
            )
        ];
    }

    public override void Validate(
        IComponentContext context, ComponentState state, IDiagnosticBag bag,
        CancellationToken cancellationToken = default
    ) => Validators.ValidateFile(context, this, state, bag);

    public override Result<RenderedComponent> Render(
        ComponentEmitContext context,
        ComponentState state,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    ) => context.Renderer.RenderFile(context, this, state, options.TypingContext, cancellationToken);
}