namespace ComponentDesigner.Nodes;

public sealed class TextInputComponentNode : ComponentNode
{
    public override string Name => "text-input";

    public override IReadOnlyList<string> Aliases { get; } = ["input"];

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ComponentProperty Id { get; }
    public ComponentProperty CustomId { get; }
    public ComponentProperty Style { get; }
    public ComponentProperty MinLength { get; }
    public ComponentProperty MaxLength { get; }
    public ComponentProperty Required { get; }
    public ComponentProperty Value { get; }
    public ComponentProperty Placeholder { get; }

    public TextInputComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            CustomId = new("customId"),
            Style = new("style", isOptional: true),
            MinLength = new("minLength", aliases: ["min"], isOptional: true),
            MaxLength = new("maxLength", aliases: ["max"], isOptional: true),
            Required = new("required", isOptional: true),
            Value = new("value", isOptional: true),
            Placeholder = new("placeholder", isOptional: true)
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
        Validators.ValidateTextInput,
        context.Renderer.RenderTextInput,
        cancellationToken
    );
}