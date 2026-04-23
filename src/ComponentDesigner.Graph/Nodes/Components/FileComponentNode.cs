namespace ComponentDesigner.Nodes;

public sealed class FileComponentNode : ComponentNode
{
    public override string Name => "file";

    public override ComponentTargetType Target => ComponentTargetType.Message;
    
    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ComponentProperty Id { get; }
    public ComponentProperty File { get; }
    public ComponentProperty IsSpoiler { get; }

    public FileComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            File = new(
                "file",
                aliases: ["url", "media"],
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            IsSpoiler = new(
                "spoiler",
                isOptional: true,
                requiresValue: false,
                kind: ComponentPropertyValueKind.SyntaxValue
            )
        ];
    }

    public override void Validate(
        IComponentContext context, ComponentState state, IDiagnosticBag bag,
        CancellationToken cancellationToken = default
    ) => Validators.ValidateFile(context, this, state, bag);
}