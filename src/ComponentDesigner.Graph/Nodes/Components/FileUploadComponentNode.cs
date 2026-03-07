namespace ComponentDesigner.Nodes;

public sealed class FileUploadComponentNode : ComponentNode
{
    public override string Name => "file-upload";

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ComponentProperty Id { get; }
    public ComponentProperty CustomId { get; }
    public ComponentProperty MinValues { get; }
    public ComponentProperty MaxValues { get; }
    public ComponentProperty Required { get; }

    public FileUploadComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            CustomId = new(
                "customId",
                autoFillMode: PropertyAutoFillMode.String
            ),
            MinValues = new(
                "min",
                aliases: ["minValues"],
                isOptional: true,
                autoFillMode: PropertyAutoFillMode.String
            ),
            MaxValues = new(
                "max",
                aliases: ["maxValues"],
                isOptional: true,
                autoFillMode: PropertyAutoFillMode.String
            ),
            Required = new(
                "required",
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
    ) => Validators.ValidateFileUpload(context, this, state, bag);

    public override Result<RenderedComponent> Render(
        ComponentEmitContext context,
        ComponentState state,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    ) => context.Renderer.RenderFileUpload(context, this, state, options.TypingContext, cancellationToken);
}