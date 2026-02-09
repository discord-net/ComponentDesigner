namespace ComponentDesigner.Nodes;

public sealed class FileComponentNode : ComponentNode
{
    public override string Name => "file";

    public override bool AllowChildrenInCX => true;

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ComponentProperty Id { get; }
    public ComponentProperty Url { get; }
    public ComponentProperty IsSpoiler { get; }

    public FileComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            Url = new(
                "url",
                aliases: ["media"]
            ),
            IsSpoiler = new(
                "spoiler",
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
    )=> ValidateAndRender(
        this,
        state,
        context,
        options,
        Validators.ValidateFile,
        context.Renderer.RenderFile,
        cancellationToken
    );
}