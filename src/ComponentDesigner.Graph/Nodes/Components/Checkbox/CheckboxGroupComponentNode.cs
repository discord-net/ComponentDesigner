namespace ComponentDesigner.Nodes;

public sealed class CheckboxGroupComponentNode : ComponentNode
{
    public override string Name => "checkbox-group";

    public override ComponentTargetType Target => ComponentTargetType.Modal;
    
    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ComponentProperty Id { get; }

    public ComponentProperty CustomId { get; }

    public ComponentProperty Options { get; }

    public ComponentProperty MinValues { get; }

    public ComponentProperty MaxValues { get; }

    public ComponentProperty Required { get; }

    public CheckboxGroupComponentNode()
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
            MinValues = new(
                "minValues",
                aliases: ["min"],
                kind: ComponentPropertyValueKind.SyntaxValue,
                isOptional: true
            ),
            MaxValues = new(
                "maxValues",
                aliases: ["max"],
                kind: ComponentPropertyValueKind.SyntaxValue,
                isOptional: true
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
    ) => Validators.ValidateCheckboxGroup(context, this, state, bag, cancellationToken);
}