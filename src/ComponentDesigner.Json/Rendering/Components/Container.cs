using System.Text.Json.Nodes;
using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    private const int CONTAINER_TYPE = 17;

    public Result<JsonNode> RenderContainer(
        IRenderContext<JsonNode> context,
        ContainerComponentNode container,
        ComponentState state,
        CancellationToken cancellationToken = default
    ) => Spec(
        context,
        state,
        cancellationToken,
        ("type", CONTAINER_TYPE),
        ("id", container.Id, Number),
        ("components", container.Components, ComponentArray),
        ("accent_color", container.AccentColor, Color),
        ("spoiler", container.IsSpoiler, Bool)
    );
}