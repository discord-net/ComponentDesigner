using System.Text.Json.Nodes;
using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    private const int THUMBNAIL_TYPE = 11;

    public Result<JsonNode> RenderThumbnail(
        IRenderContext<JsonNode> context,
        ThumbnailComponentNode thumbnail,
        ComponentState state,
        CancellationToken cancellationToken = default
    ) => Spec(
        context,
        state,
        cancellationToken,
        ("type", THUMBNAIL_TYPE),
        ("id", thumbnail.Id, Number),
        ("media", thumbnail.Media, UnfurledMediaItem),
        ("description", thumbnail.Description, String),
        ("spoiler", thumbnail.Spoiler, Bool)
    );
}