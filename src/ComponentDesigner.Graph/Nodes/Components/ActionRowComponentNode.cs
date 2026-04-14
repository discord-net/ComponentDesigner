namespace ComponentDesigner.Nodes;

public class ActionRowComponentNode : ComponentNode
{
    public override string Name => "action-row";

    public override IReadOnlyList<string> Aliases { get; } = ["row"];

    public override bool IsParentOfOtherComponents => true;

    public override ComponentTargetType Target => ComponentTargetType.Message;
    
    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ComponentProperty Id { get; }
    public ComponentProperty Components { get; }

    public ActionRowComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            Components = new(
                "components",
                kind: ComponentPropertyValueKind.ManyComponents,
                flags: ComponentPropertyFlags.FromChildren
            )
        ];
    }

    public override ComponentState? Initialize(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    )
    {
        var state = base.Initialize(context, diagnostics, cancellationToken);

        state?.SetPropertyValueToChildren(Components);

        return state;
    }

    public override void Validate(
        IComponentContext context, ComponentState state, IDiagnosticBag bag,
        CancellationToken cancellationToken = default
    ) => Validators.ValidateActionRow(context, this, state, bag);

    public override Result<RenderedComponent> Render(
        ComponentEmitContext context,
        ComponentState state,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    ) => context.Renderer.RenderActionRow(context, this, state, options.TypingContext, cancellationToken);
}