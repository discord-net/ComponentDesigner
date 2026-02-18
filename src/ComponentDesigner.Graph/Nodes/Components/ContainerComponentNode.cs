using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public sealed class ContainerComponentNode : ComponentNode
{
    public override string Name => "container";

    public override bool IsParentOfOtherComponents => true;

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ComponentProperty Id { get; }
    public ComponentProperty AccentColor { get; }
    public ComponentProperty IsSpoiler { get; }
    public ComponentProperty Components { get; }

    public ContainerComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            AccentColor = new(
                name: "accentColor",
                isOptional: true,
                aliases: ["color", "accent"]
            ),
            IsSpoiler = new(
                name: "spoiler",
                isOptional: true,
                requiresValue: false
            ),
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
        Validators.ValidateContainer,
        context.Renderer.RenderContainer,
        cancellationToken
    );
}