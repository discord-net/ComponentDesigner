namespace ComponentDesigner.Nodes;

public sealed class CheckboxComponentNode : ComponentNode
{
    public override string Name => "checkbox";

    public override ComponentTargetType Target => ComponentTargetType.Modal;

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ComponentProperty Id { get; }

    public ComponentProperty CustomId { get; }

    public ComponentProperty Default { get; }

    public CheckboxComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            CustomId = new(
                "customId",
                kind: ComponentPropertyValueKind.SyntaxValue
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
    ) => Validators.ValidateCheckbox(context, this, state, bag);
}