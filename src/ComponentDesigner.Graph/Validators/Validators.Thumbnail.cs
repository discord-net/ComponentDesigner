using ComponentDesigner.Nodes;

namespace ComponentDesigner;

partial class Validators
{
    public const int THUMBNAIL_DESCRIPTION_MAX_LENGTH = 1024;
    
    public static void ValidateThumbnail(
        IComponentContext context,
        ThumbnailComponentNode thumbnail,
        ComponentState state,
        IDiagnosticBag bag
    )
    {
        ValidateGenericComponent(context, thumbnail, state, bag, isParentOfOtherComponents: false);

        StringRange(
            context, state.GetPropertyValue(thumbnail.Description), bag,
            upper: THUMBNAIL_DESCRIPTION_MAX_LENGTH
        );
    }
}