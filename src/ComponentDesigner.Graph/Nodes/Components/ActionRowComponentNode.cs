namespace ComponentDesigner.Nodes;

public class ActionRowComponentNode : ComponentNode
{
    public override string Name => "action-row";

    public override IReadOnlyList<string> Aliases { get; } = ["row"];

    public override bool IsParentOfOtherComponents => true;

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ComponentProperty Id { get; }
    public ComponentProperty Components { get; }

    public ActionRowComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            
            // validated manually
            Components = new("components")
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

    public override Result<RenderedComponent> Emit(
        ComponentState state,
        ComponentEmitContext context,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    ) => ValidateAndRender(
        this,
        state,
        context,
        options,
        Validators.ValidateActionRow,
        context.Renderer.RenderActionRow,
        cancellationToken
    );
}