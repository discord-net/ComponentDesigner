namespace ComponentDesigner.Nodes;

public class CheckboxGroupOptionComponentNode : ComponentNode
{
    public override string Name => "checkbox-group-option";

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ComponentProperty Value { get; }

    public ComponentProperty Label { get; }

    public ComponentProperty Description { get; }

    public ComponentProperty Default { get; }

    public CheckboxGroupOptionComponentNode()
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
        IComponentContext context,
        ComponentState state,
        IDiagnosticBag bag,
        CancellationToken cancellationToken = default
    ) => Validators.ValidateCheckboxGroupOption(context, this, state, bag);

    public override Result<RenderedComponent> Render(
        ComponentEmitContext context,
        ComponentState state,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    ) => context.Renderer.RenderCheckboxGroupOption(context, this, state, options.TypingContext, cancellationToken);
}