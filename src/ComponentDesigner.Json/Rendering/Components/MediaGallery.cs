using System.Text.Json.Nodes;
using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    public const int MEDIA_GALLERY_TYPE = 12;
    public Result<RenderedComponent> RenderMediaGalleryItem(
        IRendererContext context,
        MediaGalleryItemComponentNode mediaGalleryItem,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => Build(
        context,
        state,
        cancellationToken,
        ("media", mediaGalleryItem.Media, UnfurledMediaItem),
        ("description", mediaGalleryItem.Description, String),
        ("spoiler", mediaGalleryItem.IsSpoiler, Bool)
    );

    public Result<RenderedComponent> RenderMediaGallery(
        IRendererContext context,
        MediaGalleryComponentNode mediaGallery,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => Build(
        context,
        state,
        cancellationToken,
        [("type", MEDIA_GALLERY_TYPE)],
        ("id", mediaGallery.Id, Number),
        ("items", mediaGallery.Items, Components)
    );
}