using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    private const int ACTION_ROW_TYPE = 1;

    public Result<RenderedComponent> RenderActionRow(
        IRendererContext context,
        ActionRowComponentNode actionRow,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => Build(
        context,
        state,
        cancellationToken,
        [("type", ACTION_ROW_TYPE)],
        ("id", actionRow.Id, Number),
        ("components", actionRow.Components, Components)
    );
}