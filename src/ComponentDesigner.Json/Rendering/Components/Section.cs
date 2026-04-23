using System.Text.Json.Nodes;
using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    private const int SECTION_TYPE = 9;

    public Result<JsonNode> RenderSection(
        IRenderContext<JsonNode> context,
        SectionComponentNode section,
        ComponentState state,
        CancellationToken cancellationToken = default
    ) => Spec(
        context,
        state,
        cancellationToken,
        ("type", SECTION_TYPE),
        ("id", section.Id, Number),
        ("components", section.Components, ComponentArray),
        ("accessory", section.Accessory, Component)
    );
}