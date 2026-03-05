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
            Accessory = new("accessory"),
            Components = new("components")
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

        ComponentPropertyValue? accessory = null;

        foreach (var child in element.Children)
        {
            if (child is CXElement { Identifier: "accessory" } accessoryElement)
            {
                ExtractChildAccessory(
                    context,
                    this,
                    diagnostics,
                    accessoryElement,
                    ref accessory,
                    cancellationToken
                );
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

        if (accessory is not null)
            state.SetPropertyValue(Accessory, accessory);

        return state;

        static void ExtractChildAccessory(
            ComponentNodeInitializationContext context,
            SectionComponentNode self,
            IDiagnosticBag bag,
            CXElement accessoryElement,
            ref ComponentPropertyValue? result,
            CancellationToken cancellationToken
        )
        {
            // do we already have an accessory?
            if (result is not null)
            {
                bag.Add(
                    Diagnostic
                        .DuplicatePropertyValue(self.Accessory)
                        .At(accessoryElement.IdentifierTextSpanOrElementTextSpan)
                );
                return;
            }

            if (accessoryElement.Children.Count is 0)
            {
                bag.Add(
                    Diagnostic
                        .ComponentRequiresAtLeastOneChild(accessoryElement)
                        .At(accessoryElement)
                );
                return;
            }

            var accessoryNodes = context.PushAsChildren(
                accessoryElement.Children,
                cancellationToken
            );

            if (accessoryNodes.Count is 0)
            {
                // diagnostics should come from the component graph
                return;
            }

            if (accessoryNodes.Count is not 1)
            {
                bag.Add(
                    Diagnostic
                        .TooManyChildren(
                            accessoryElement,
                            1
                        )
                        .At(
                            CXTextSpan.FromBounds(
                                accessoryNodes[1].State.TextSpan.Start,
                                accessoryNodes[accessoryNodes.Count - 1].State.TextSpan.End
                            )
                        )
                );

                return;
            }

            result = new ComponentPropertyValue.Component(
                self.Accessory,
                accessoryNodes[0]
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