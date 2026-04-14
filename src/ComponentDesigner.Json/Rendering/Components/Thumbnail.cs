using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    private const int THUMBNAIL_TYPE = 11;

    public Result<RenderedComponent> RenderThumbnail(
        IRendererContext context,
        ThumbnailComponentNode thumbnail,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => Build(
        context,
        state,
        cancellationToken,
        [("type", THUMBNAIL_TYPE)],
        ("id", thumbnail.Id, Number),
        ("media", thumbnail.Media, UnfurledMediaItem),
        ("description", thumbnail.Description, String),
        ("spoiler", thumbnail.IsSpoiler, Bool)
    );
}