using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public sealed class MediaGalleryComponentNode : ComponentNode
{
    public override string Name => "media-gallery";

    public override IReadOnlyList<string> Aliases { get; } = ["gallery"];

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public override bool IsParentOfOtherComponents => true;

    public ComponentProperty Id { get; }
    public ComponentProperty Items { get; }

    public MediaGalleryComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            Items = new("items")
        ];
    }

    public override void RegisterGraphNode(
        ComponentGraphInitializationContext context,
        CancellationToken cancellationToken = default
    )
    {
        base.RegisterGraphNode(context, includeElementChildren: false, cancellationToken);
    }

    public override ComponentState? Initialize(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    )
    {
        var state = base.Initialize(context, diagnostics, cancellationToken);

        if (state is not null && context.CXNode is CXElement { Children.Count: > 0 } element)
        {
            var values = new List<ComponentPropertyValue>();

            foreach (var childCX in element.Children)
            {
                switch (childCX)
                {
                    case CXElement childElement:
                        var children = context.PushAsChildren(childElement, cancellationToken);

                        values.AddRange(
                            children
                                .Select(x =>
                                    new ComponentPropertyValue.Component(
                                        Items,
                                        x
                                    )
                                )
                        );

                        break;
                    case CXValue value:
                        values.Add(new ComponentPropertyValue.SyntaxValue(Items, value));
                        break;
                    default:
                        // TODO: error?
                        break;
                }
            }

            if (values.Count > 0)
            {
                state.SetPropertyValue(
                    Items,
                    values.Count is 1 ? values[0] : new ComponentPropertyValue.Many(Items, values)
                );
            }
        }

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
        Validators.ValidateMediaGallery,
        context.Renderer.RenderMediaGallery,
        cancellationToken
    );
}