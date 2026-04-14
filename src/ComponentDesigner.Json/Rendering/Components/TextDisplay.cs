using System.Text.Json.Nodes;
using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    private const int TEXT_DISPLAY_TYPE = 10;

    public Result<RenderedComponent> RenderTextDisplay(
        IRendererContext context,
        TextDisplayComponentNode textDisplay,
        TextDisplayState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    )
    {
        return Build(
            context,
            state,
            cancellationToken,
            [("type", TEXT_DISPLAY_TYPE)],
            ("id", textDisplay.Id, Number),
            ("content", textDisplay.Content, RenderContent)
        );

        Result<JsonNode> RenderContent(
            IRendererContext context,
            ComponentPropertyValue propertyValue,
            CancellationToken cancellationToken
        )
        {
            if (propertyValue.AsSingle is ComponentPropertyValue.Component component)
            {
                return context
                    .RenderGraphNode(component.GraphNode, cancellationToken: cancellationToken)
                    .Map(render =>
                    {
                        if (render is not RenderedJsonComponent json)
                            return JsonValue.Create(render.Source);

                        return json.JsonNode;
                    });
            }

            return String(context, propertyValue, cancellationToken);
        }
    }
}