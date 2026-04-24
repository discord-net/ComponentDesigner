using System.Text.Json.Nodes;
using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    private const int FILE_TYPE = 13;

    public Result<JsonNode> RenderFile(
        IRenderContext<JsonNode> context,
        FileComponentNode file,
        ComponentState state,
        CancellationToken cancellationToken = default
    ) => Spec(
        context,
        state,
        cancellationToken,
        ("type", FILE_TYPE),
        ("id", file.Id, Number),
        ("file", file.File, UnfurledMediaItem),
        ("spoiler", file.Spoiler, Bool)
    );
}