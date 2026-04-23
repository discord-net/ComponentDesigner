using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public sealed class MediaGalleryComponentNode : ComponentNode
{
    public override string Name => "media-gallery";

    public override IReadOnlyList<string> Aliases { get; } = ["gallery"];

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public override ComponentTargetType Target => ComponentTargetType.Message;
    
    public ComponentProperty Id { get; }
    public ComponentProperty Items { get; }

    public MediaGalleryComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            Items = new(
                "items",
                kind: ComponentPropertyValueKind.ManyComponents | ComponentPropertyValueKind.Interpolation,
                flags: ComponentPropertyFlags.FromChildren
            )
        ];
    }

    public override void RegisterGraphNode(
        ComponentGraphInitializationContext context,
        CancellationToken cancellationToken = default
    ) => base.RegisterGraphNode(context, includeElementChildren: false, cancellationToken);

    public override ComponentState? CreateState(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    )
    {
        var state = base.CreateState(context, diagnostics, cancellationToken);

        if (state is not null && context.CXNode is CXElement { Children.Count: > 0 } element)
        {
            using var _ = List<ComponentPropertyValue>.Pooled(out var values);
            values.Clear();

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
                                        state.ChildSource,
                                        Items,
                                        x
                                    )
                                )
                        );

                        break;
                    case CXValue value:
                        values.AddRange(
                            state
                                .BuildPropertyValueFromSyntax(
                                    context,
                                    Items,
                                    state.ChildSource,
                                    value,
                                    value.TextSpan,
                                    cancellationToken
                                )
                                .AsFlattened
                        );
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
                    values.Count is 1
                        ? values[0]
                        : new ComponentPropertyValue.Many(state.ChildSource, Items, [..values])
                );
            }
        }

        return state;
    }

    public override void Validate(
        IComponentContext context, ComponentState state, IDiagnosticBag bag,
        CancellationToken cancellationToken = default
    ) => Validators.ValidateMediaGallery(context, this, state, bag, cancellationToken);
}