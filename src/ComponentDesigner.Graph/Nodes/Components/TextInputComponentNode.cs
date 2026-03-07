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
            CustomId = new(
                "customId",
                autoFillMode: PropertyAutoFillMode.String
            ),
            Style = new(
                "style",
                isOptional: true,
                autoFillMode: PropertyAutoFillMode.String,
                autoFillChoices: ["short", "paragraph"]
            ),
            MinLength = new(
                "minLength",
                aliases: ["min"],
                isOptional: true
            ),
            MaxLength = new(
                "maxLength",
                aliases: ["max"],
                isOptional: true,
                autoFillMode: PropertyAutoFillMode.String
            ),
            Required = new(
                "required",
                isOptional: true,
                autoFillMode: PropertyAutoFillMode.String,
                autoFillChoices: ["true", "false"]
            ),
            Value = new(
                "value",
                isOptional: true,
                autoFillMode: PropertyAutoFillMode.String
            ),
            Placeholder = new(
                "placeholder",
                isOptional: true,
                autoFillMode: PropertyAutoFillMode.String
            )
        ];
    }

    public override void Validate(
        IComponentContext context, ComponentState state, IDiagnosticBag bag,
        CancellationToken cancellationToken = default
    ) => Validators.ValidateTextInput(context, this, state, bag);

    public override Result<RenderedComponent> Render(
        ComponentEmitContext context,
        ComponentState state,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    ) => context.Renderer.RenderTextInput(context, this, state, options.TypingContext, cancellationToken);
}