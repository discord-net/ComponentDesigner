using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    private const int LABEL_TYPE = 18;

    public Result<RenderedComponent> RenderLabel(
        IRendererContext context,
        LabelComponentNode label,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => Build(
        context,
        state,
        cancellationToken,
        [("type", LABEL_TYPE)],
        ("id", label.Id, Number),
        ("label", label.Label, String),
        ("description", label.Description, String),
        ("component", label.Component, Component)
    );
}