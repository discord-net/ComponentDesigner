using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    private const int SECTION_TYPE = 9;

    public Result<RenderedComponent> RenderSection(
        IRendererContext context,
        SectionComponentNode section,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => Build(
        context,
        state,
        cancellationToken,
        [("type", SECTION_TYPE)],
        ("id", section.Id, Number),
        ("components", section.Components, Components),
        ("accessory", section.Accessory, Component)
    );
}