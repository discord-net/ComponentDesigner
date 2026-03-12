using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public sealed class SectionComponentNode : ComponentNode
{
    public override string Name => "section";

    public override bool IsParentOfOtherComponents => true;

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ComponentProperty Id { get; }
    public ComponentProperty Accessory { get; }
    public ComponentProperty Components { get; }

    public SectionComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            Accessory = new(
                "accessory",
                kind: ComponentPropertyValueKind.Component
            ),
            Components = new(
                "components",
                kind: ComponentPropertyValueKind.ManyComponents
            )
        ];
    }

    public override void RegisterGraphNode(
        ComponentGraphInitializationContext context,
        CancellationToken cancellationToken = default
    ) => base.RegisterGraphNode(context, includeElementChildren: false, cancellationToken);

    public override ComponentState? Initialize(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    )
    {
        if (
            context.CXNode is not CXElement element ||
            base.Initialize(context, diagnostics, cancellationToken) is not { } state
        ) return null;

        using var _ = ObjectPool<List<ICXNode>>.GetScoped(out var children);
        children.Clear();

        foreach (var child in element.Children)
        {
            if (child is CXElement { Identifier: "accessory" } accessoryElement)
            {
                ExtractChildAccessory(accessoryElement);
                continue;
            }

            children.Add(child);
        }

        if (children.Count > 0)
        {
            state.SetPropertyValueToChildren(
                Components,
                context.PushAsChildren(
                    children,
                    cancellationToken
                )
            );
        }

        return state;

        void ExtractChildAccessory(
            CXElement accessoryElement
        )
        {
            // do we already have an accessory?
            if (!state.GetPropertyValue(Accessory).IsNone)
            {
                diagnostics.Add(
                    Diagnostic
                        .DuplicatePropertyValue(this.Accessory)
                        .At(accessoryElement.IdentifierTextSpanOrElementTextSpan)
                );
                return;
            }

            if (accessoryElement.Children.Count is 0)
            {
                diagnostics.Add(
                    Diagnostic
                        .ComponentRequiresAtLeastOneChild(accessoryElement)
                        .At(accessoryElement)
                );
                return;
            }

            state.SetPropertyValueToChildren(
                Accessory,
                context.PushAsChildren(
                    accessoryElement.Children,
                    cancellationToken
                )
            );
        }
    }

    public override void Validate(
        IComponentContext context, ComponentState state, IDiagnosticBag bag,
        CancellationToken cancellationToken = default
    ) => Validators.ValidateSection(context, this, state, bag);

    public override Result<RenderedComponent> Render(
        ComponentEmitContext context,
        ComponentState state,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    ) => context.Renderer.RenderSection(context, this, state, options.TypingContext, cancellationToken);
}