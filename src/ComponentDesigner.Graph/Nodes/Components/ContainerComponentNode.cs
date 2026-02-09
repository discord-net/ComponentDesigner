using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public sealed class ContainerComponentNode : ComponentNode
{
    public override string Name => "container";

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ComponentProperty Id { get; }
    public ComponentProperty AccentColor { get; }
    public ComponentProperty IsSpoiler { get; }

    public ContainerComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            AccentColor = new(
                name: "accentColor",
                isOptional: true,
                aliases: ["color", "accent"]
            ),
            IsSpoiler = new(
                name: "spoiler",
                isOptional: true,
                requiresValue: false
            )
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
        Validators.ValidateContainer,
        context.Renderer.RenderContainer,
        cancellationToken
    );
}