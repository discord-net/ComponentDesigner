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
            Divider = new("divider", isOptional: true),
            Spacing = new("spacing", aliases: ["size"], isOptional: true)
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
        Validators.ValidateSeparator,
        context.Renderer.RenderSeparator,
        cancellationToken
    );
}