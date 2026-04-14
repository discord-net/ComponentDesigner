using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public sealed class ContainerComponentNode : ComponentNode
{
    public override string Name => "container";

    public override bool IsParentOfOtherComponents => true;

    public override ComponentTargetType Target => ComponentTargetType.Message;
    
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
                aliases: ["color", "accent"],
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            IsSpoiler = new(
                name: "spoiler",
                isOptional: true,
                requiresValue: false,
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
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
    ) => Validators.ValidateContainer(context, this, state, bag);

    public override Result<RenderedComponent> Render(
        ComponentEmitContext context,
        ComponentState state,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    ) => context.Renderer.RenderContainer(context, this, state, options.TypingContext, cancellationToken);
}