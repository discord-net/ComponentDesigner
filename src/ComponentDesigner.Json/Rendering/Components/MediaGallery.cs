using System.Text.Json.Nodes;
using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    public const int MEDIA_GALLERY_TYPE = 12;
    public Result<JsonNode> RenderMediaGalleryItem(
        IRenderContext<JsonNode> context,
        MediaGalleryItemComponentNode mediaGalleryItem,
        ComponentState state,
        CancellationToken cancellationToken = default
    ) => Spec(
        context,
        state,
        cancellationToken,
        ("media", mediaGalleryItem.Media, UnfurledMediaItem),
        ("description", mediaGalleryItem.Description, String),
        ("spoiler", mediaGalleryItem.Spoiler, Bool)
    );

    public Result<JsonNode> RenderMediaGallery(
        IRenderContext<JsonNode> context,
        MediaGalleryComponentNode mediaGallery,
        ComponentState state,
        CancellationToken cancellationToken = default
    ) => Spec(
        context,
        state,
        cancellationToken,
        ("type", MEDIA_GALLERY_TYPE),
        ("id", mediaGallery.Id, Number),
        ("items", mediaGallery.Items, ComponentArray)
    );
}