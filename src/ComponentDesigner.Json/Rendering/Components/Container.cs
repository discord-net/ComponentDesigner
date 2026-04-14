using System.Text.Json.Nodes;
using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    private const int CONTAINER_TYPE = 17;

    public Result<RenderedComponent> RenderContainer(
        IRendererContext context,
        ContainerComponentNode container,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => Build(
        context,
        state,
        cancellationToken,
        [("type", CONTAINER_TYPE)],
        ("id", container.Id, Number),
        ("components", container.Components, Components),
        ("accent_color", container.AccentColor, Color),
        ("spoiler", container.IsSpoiler, Bool)
    );
}