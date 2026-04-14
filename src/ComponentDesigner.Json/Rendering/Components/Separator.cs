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

    public Result<RenderedComponent> RenderSeparator(
        IRendererContext context,
        SeparatorComponentNode separator,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => Build(
        context,
        state,
        cancellationToken,
        [("type", SEPARATOR_TYPE)],
        ("id", separator.Id, Number),
        ("divider", separator.Divider, Bool),
        ("spacing", separator.Spacing, SeparatorSpacingEnum)
    );
}