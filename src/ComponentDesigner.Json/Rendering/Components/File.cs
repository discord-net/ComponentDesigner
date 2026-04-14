using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    private const int FILE_TYPE = 13;

    public Result<RenderedComponent> RenderFile(
        IRendererContext context,
        FileComponentNode file,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => Build(
        context,
        state,
        cancellationToken,
        [("type", FILE_TYPE)],
        ("id", file.Id, Number),
        ("file", file.File, UnfurledMediaItem),
        ("spoiler", file.IsSpoiler, Bool)
    );
}