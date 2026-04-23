using System.Text.Json.Nodes;
using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    private const int SEPARATOR_TYPE = 14;

    private static readonly PropertyRenderer SeparatorSpacingEnum = Enum(
        ("small", 1),
        ("large", 2)
    );

    public Result<JsonNode> RenderSeparator(
        IRenderContext<JsonNode> context,
        SeparatorComponentNode separator,
        ComponentState state,
        CancellationToken cancellationToken = default
    ) => Spec(
        context,
        state,
        cancellationToken,
        ("type", SEPARATOR_TYPE),
        ("id", separator.Id, Number),
        ("divider", separator.Divider, Bool),
        ("spacing", separator.Spacing, SeparatorSpacingEnum)
    );
}