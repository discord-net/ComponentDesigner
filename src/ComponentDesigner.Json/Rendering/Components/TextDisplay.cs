using System.Text.Json.Nodes;
using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    private const int TEXT_DISPLAY_TYPE = 10;

    public Result<JsonNode> RenderTextDisplay(
        IRenderContext<JsonNode> context,
        TextDisplayComponentNode textDisplay,
        TextDisplayState state,
        CancellationToken cancellationToken = default
    )
    {
        return Spec(
            context,
            state,
            cancellationToken,
            ("type", TEXT_DISPLAY_TYPE),
            ("id", textDisplay.Id, Number),
            ("content", textDisplay.Content, RenderContent)
        );

        Result<JsonNode> RenderContent(
            IRenderContext<JsonNode> context,
            ComponentPropertyValue propertyValue,
            CancellationToken cancellationToken
        )
        {
            if (propertyValue.AsSingle is ComponentPropertyValue.Component component)
            {
                return component.GraphNode.Render(context, cancellationToken);
            }

            return String.GetJsonNode(context, propertyValue, cancellationToken);
        }
    }
}