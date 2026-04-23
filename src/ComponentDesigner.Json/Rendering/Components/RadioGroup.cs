using System;
using System.Text.Json.Nodes;
using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    private const int RADIO_GROUP_TYPE = 21;
    
    public Result<JsonNode> RenderRadioGroup(
        IRenderContext<JsonNode> context,
        RadioGroupComponentNode radioGroup,
        ComponentState state,
        CancellationToken cancellationToken = default
    ) => Spec(
        context,
        state,
        cancellationToken,
        ("type", RADIO_GROUP_TYPE),
        ("id", radioGroup.Id, Number),
        ("custom_id", radioGroup.CustomId, String),
        ("options", radioGroup.Options, ComponentArray),
        ("required", radioGroup.Required, Bool)
    );
    
    public Result<JsonNode> RenderRadioGroupOption(
        IRenderContext<JsonNode> context,
        RadioGroupOptionComponentNode radioGroupOption,
        ComponentState state,
        CancellationToken cancellationToken = default
    ) => Spec(
        context,
        state,
        cancellationToken,
        ("value", radioGroupOption.Value, String),
        ("label", radioGroupOption.Label, String),
        ("description", radioGroupOption.Description, String),
        ("default", radioGroupOption.Default, Bool)
    );
}