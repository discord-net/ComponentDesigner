using System.Text.Json.Nodes;
using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    private const int LABEL_TYPE = 18;

    public Result<JsonNode> RenderLabel(
        IRenderContext<JsonNode> context,
        LabelComponentNode label,
        ComponentState state,
        CancellationToken cancellationToken = default
    ) => Spec(
        context,
        state,
        cancellationToken,
        ("type", LABEL_TYPE),
        ("id", label.Id, Number),
        ("label", label.Label, String),
        ("description", label.Description, String),
        ("component", label.Component, Component)
    );
}