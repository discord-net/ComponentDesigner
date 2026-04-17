namespace ComponentDesigner.Nodes;

public sealed class RadioGroupOptionComponentNode : ComponentNode
{
    public override string Name => "radio-group-option";

    public override ComponentTargetType Target => ComponentTargetType.Modal;
    
    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ComponentProperty Value { get; }

    public ComponentProperty Label { get; }

    public ComponentProperty Description { get; }

    public ComponentProperty Default { get; }

    public RadioGroupOptionComponentNode()
    {
        Properties =
        [
            Value = new(
                "value",
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            Label = new(
                "label",
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            Description = new(
                "description",
                kind: ComponentPropertyValueKind.SyntaxValue,
                isOptional: true
            ),
            Default = new(
                "default",
                kind: ComponentPropertyValueKind.SyntaxValue,
                isOptional: true,
                requiresValue: false
            )
        ];
    }

    public override void Validate(
        IComponentContext context, ComponentState state, IDiagnosticBag bag,
        CancellationToken cancellationToken = default
    ) => Validators.ValidateRadioGroupOption(context, this, state, bag);

    public override Result<RenderedComponent> Render(
        ComponentEmitContext context,
        ComponentState state,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    ) => context.Renderer.RenderRadioGroupOption(context, this, state, options.TypingContext, cancellationToken);
}