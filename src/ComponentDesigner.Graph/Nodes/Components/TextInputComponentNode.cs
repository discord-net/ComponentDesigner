namespace ComponentDesigner.Nodes;

public sealed class TextInputComponentNode : ComponentNode
{
    public override string Name => "text-input";

    public override IReadOnlyList<string> Aliases { get; } = ["input"];
    
    public override ComponentTargetType Target => ComponentTargetType.Modal;
    
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
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            Style = new(
                "style",
                isOptional: true,
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            MinLength = new(
                "minLength",
                aliases: ["min"],
                isOptional: true,
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            MaxLength = new(
                "maxLength",
                aliases: ["max"],
                isOptional: true,
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            Required = new(
                "required",
                isOptional: true,
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            Value = new(
                "value",
                isOptional: true,
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            Placeholder = new(
                "placeholder",
                isOptional: true,
                kind: ComponentPropertyValueKind.SyntaxValue
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
    ) => context.Renderer
        .RenderTextInput(context, this, state, options.TypingContext, cancellationToken);
}