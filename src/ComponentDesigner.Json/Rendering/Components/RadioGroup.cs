using System;
using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    private const int RADIO_GROUP_TYPE = 21;
    
    public Result<RenderedComponent> RenderRadioGroup(
        IRendererContext context,
        RadioGroupComponentNode radioGroup,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => Build(
        context,
        state,
        cancellationToken,
        [("type", RADIO_GROUP_TYPE)],
        ("id", radioGroup.Id, Number),
        ("custom_id", radioGroup.CustomId, String),
        ("options", radioGroup.Options, Components),
        ("required", radioGroup.Required, Bool)
    );
    
    public Result<RenderedComponent> RenderRadioGroupOption(
        IRendererContext context,
        RadioGroupOptionComponentNode radioGroupOption,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => Build(
        context,
        state,
        cancellationToken,
        ("value", radioGroupOption.Value, String),
        ("label", radioGroupOption.Label, String),
        ("description", radioGroupOption.Description, String),
        ("default", radioGroupOption.Default, Bool)
    );
}