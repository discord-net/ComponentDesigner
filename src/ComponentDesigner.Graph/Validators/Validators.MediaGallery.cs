using ComponentDesigner.Nodes;

namespace ComponentDesigner;

partial class Validators
{
    public const int MEDIA_GALLERY_MAX_ITEMS = 10;

    public static void ValidateMediaGallery(
        IComponentContext context,
        MediaGalleryComponentNode gallery,
        ComponentState state,
        IDiagnosticBag bag,
        CancellationToken cancellationToken
    )
    {
        ValidateGenericComponent(gallery, state, bag);

        var items = state.GetPropertyValue(gallery.Items);

        if (
            !context.Implementation.TryAnalyzeNumberOfValues(
                context,
                gallery,
                items,
                cancellationToken,
                out var numberOfItems
            )
        )
        {
            numberOfItems = items.AsFlattened.OfType<ComponentPropertyValue.Component>().Count();
        }

        if (numberOfItems.Upper > MEDIA_GALLERY_MAX_ITEMS)
        {
            bag.Add(
                Diagnostic
                    .TooManyChildren(gallery, MEDIA_GALLERY_MAX_ITEMS)
                    .At(state.ElementIdentifierTextSpanOrBetter)
            );
        }
        foreach (var child in items.AsFlattened.OfType<ComponentPropertyValue.Component>())
        {
            if (!IsValidChild(child.GraphNode.Component))
            {
                bag.Add(
                    Diagnostic.InvalidChildOfComponent(gallery, child.GraphNode.Component).At(child)
                );
            }
        }

        static bool IsValidChild(IComponentNode componentNode)
            => componentNode is IDynamicComponentNode or MediaGalleryItemComponentNode;
    }

    public static void ValidateMediaGalleryItem(
        IComponentContext context,
        MediaGalleryItemComponentNode item,
        ComponentState state,
        IDiagnosticBag bag
    )
    {
        ValidateGenericComponent(item, state, bag);
    }
}