using System.Text.Json.Nodes;
using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    private const int TEXT_INPUT_TYPE = 4;

    private static readonly PropertyRenderer TextInputStyle = Enum(
        ("short", 1),
        ("paragraph", 2)
    );

    public Result<JsonNode> RenderTextInput(
        IRenderContext<JsonNode> context,
        TextInputComponentNode textInput,
        ComponentState state,
        CancellationToken cancellationToken = default
    ) => Spec(
        context,
        state,
        cancellationToken,
        ("type", TEXT_INPUT_TYPE),
        ("id", textInput.Id, Number),
        ("custom_id", textInput.CustomId, String),
        ("style", textInput.Style, TextInputStyle),
        ("min_length", textInput.MinLength, Number),
        ("max_length", textInput.MaxLength, Number),
        ("required", textInput.Required, Bool),
        ("value", textInput.Value, String),
        ("placeholder", textInput.Placeholder, String)
    );
}