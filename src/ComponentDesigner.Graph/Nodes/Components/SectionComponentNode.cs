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
    )
    {
        context.Push(
            this,
            cxNode: context.CXNode
        );
    }

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

        if (element.Children.Count > 1)
        {
            diagnostics.Add(
                CXTextSpan.FromBounds(
                    element.Children[1].Span.Start,
                    element.Children[element.Children.Count - 1].Span.End
                ).Report(
                    Diagnostic.OnlyOneChildAllowed(this)
                )
            );

            return null;
        }

        if (element.Children.Count > 0) state.SetPropertyValueToChildren(Accessory);

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
        Validators.ValidateSection,
        context.Renderer.RenderSection,
        cancellationToken
    );
}