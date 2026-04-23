using System.Text.Json.Nodes;
using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    private const int ACTION_ROW_TYPE = 1;

    public Result<JsonNode> RenderActionRow(
        IRenderContext<JsonNode> context,
        ActionRowComponentNode actionRow,
        ComponentState state,
        CancellationToken cancellationToken = default
    ) => Spec(
        context,
        state,
        cancellationToken,
        ("type", ACTION_ROW_TYPE),
        ("id", actionRow.Id, Number),
        ("components", actionRow.Components, ComponentArray)
    );
}