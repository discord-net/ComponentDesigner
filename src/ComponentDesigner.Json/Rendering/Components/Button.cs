using System.Text.Json.Nodes;
using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    private const int BUTTON_TYPE = 2;

    private const int BUTTON_LINK_STYLE = 5;
    private const int BUTTON_PREMIUM_STYLE = 6;
    
    private static readonly PropertyRenderer ButtonStyle = Enum(
        ("primary", 1),
        ("secondary", 2),
        ("success", 3),
        ("danger", 4),
        ("link", BUTTON_LINK_STYLE),
        ("premium", BUTTON_PREMIUM_STYLE)
    );
    
    public Result<JsonNode> RenderButton(
        IRenderContext<JsonNode> context, 
        ButtonComponentNode button,
        ButtonState state,
        CancellationToken cancellationToken = default
    )
    {
        return Spec(
            context,
            state,
            cancellationToken,
            ("type", BUTTON_TYPE),
            ("id", button.Id, Number),
            ("style", button.Style, RenderStyle),
            ("label", button.Label, String),
            ("emoji", button.Emoji, Emoji),
            ("custom_id", button.CustomId, String),
            ("sku_id", button.SkuId, String),
            ("url", button.Url, String),
            ("disabled", button.Disabled, Bool)
        );

        Result<JsonNode> RenderStyle(
            IRenderContext<JsonNode> context,
            ComponentPropertyValue propertyValue,
            CancellationToken cancellationToken
        )
        {
            if (state.InferredKind is ButtonKind.Link)
                return JsonValue.Create(BUTTON_LINK_STYLE);

            if (state.InferredKind is ButtonKind.Premium)
                return JsonValue.Create(BUTTON_PREMIUM_STYLE);

            return ButtonStyle(context, propertyValue, cancellationToken);
        }
    }
}