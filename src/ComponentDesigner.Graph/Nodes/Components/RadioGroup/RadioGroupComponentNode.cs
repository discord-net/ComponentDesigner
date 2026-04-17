namespace ComponentDesigner.Nodes;

public sealed class RadioGroupComponentNode : ComponentNode
{
    public override string Name => "radio-group";

    public override ComponentTargetType Target => ComponentTargetType.Modal;
    
    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ComponentProperty Id { get; }
    public ComponentProperty CustomId { get; }
    public ComponentProperty Options { get; }
    public ComponentProperty Required { get; }

    public RadioGroupComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            CustomId = new(
                "customId",
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            Options = new(
                "options",
                kind: ComponentPropertyValueKind.ManyComponents | ComponentPropertyValueKind.Interpolation,
                flags: ComponentPropertyFlags.FromChildren
            ),
            Required = new(
                "required",
                kind: ComponentPropertyValueKind.SyntaxValue,
                isOptional: true,
                requiresValue: false
            )
        ];
    }

    public override ComponentState? CreateState(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    )
    {
        var state = base.CreateState(context, diagnostics, cancellationToken);
        
        state?.SetPropertyValueToChildren(Options);

        return state;
    }

    public override void Validate(
        IComponentContext context, 
        ComponentState state, 
        IDiagnosticBag bag,
        CancellationToken cancellationToken = default
    ) => Validators.ValidateRadioGroup(context, this, state, bag, cancellationToken);

    public override Result<RenderedComponent> Render(
        ComponentEmitContext context,
        ComponentState state,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    ) => context.Renderer.RenderRadioGroup(context, this, state, options.TypingContext, cancellationToken);
}