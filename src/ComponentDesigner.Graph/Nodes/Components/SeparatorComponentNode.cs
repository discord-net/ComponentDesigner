namespace ComponentDesigner.Nodes;

public sealed class SeparatorComponentNode : ComponentNode
{
    public override string Name => "separator";

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ComponentProperty Id { get; }
    public ComponentProperty Divider { get; }
    public ComponentProperty Spacing { get; }

    public SeparatorComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            Divider = new(
                "divider",
                isOptional: true,
                autoFillMode: PropertyAutoFillMode.String,
                autoFillChoices: ["true", "false"]
            ),
            Spacing = new(
                "spacing",
                aliases: ["size"],
                isOptional: true,
                autoFillMode: PropertyAutoFillMode.String
            )
        ];
    }

    public override void Validate(
        IComponentContext context, ComponentState state, IDiagnosticBag bag,
        CancellationToken cancellationToken = default
    ) => Validators.ValidateSeparator(context, this, state, bag);

    public override Result<RenderedComponent> Render(
        ComponentEmitContext context,
        ComponentState state,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    ) => context.Renderer.RenderSeparator(context, this, state, options.TypingContext, cancellationToken);
}