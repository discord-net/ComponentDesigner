using ComponentDesigner.Nodes;

namespace ComponentDesigner;

partial class Validators
{
    public const int MEDIA_GALLERY_MAX_ITEMS = 10;

    public static void ValidateMediaGallery(
        IComponentContext context,
        MediaGalleryComponentNode gallery,
        ComponentState state,
        IDiagnosticBag bag
    )
    {
        ValidateElementStructure(gallery, state, bag);
        ValidateProperty(gallery, state.GetPropertyValue(gallery.Id), bag);
        ReportDiagnosticsForUnknownProperties(gallery, state, bag);
        
        if (state is { HasGraphChildren: true, Children.Count: > MEDIA_GALLERY_MAX_ITEMS })
        {
            bag.Add(
                Diagnostic
                    .TooManyChildren(gallery, MEDIA_GALLERY_MAX_ITEMS)
                    .At(
                        CXTextSpan.FromBounds(
                            state.Children[MEDIA_GALLERY_MAX_ITEMS].State.TextSpan.Start,
                            state.Children[state.Children.Count - 1].State.TextSpan.End
                        )
                    )
            );

            return;
        }

        foreach (var child in state.Children)
        {
            if (!IsValidChild(child.Component))
            {
                bag.Add(
                    Diagnostic.InvalidChildOfComponent(gallery, child.Component).At(child.State.TextSpan)
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
    }
}