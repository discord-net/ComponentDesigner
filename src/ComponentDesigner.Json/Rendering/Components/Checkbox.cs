using System;
using System.Text.Json.Nodes;
using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    public const int CHECKBOX_TYPE = 23;
    public const int CHECKBOX_GROUP_TYPE = 22;

    public Result<JsonNode> RenderCheckbox(
        IRenderContext<JsonNode> context,
        CheckboxComponentNode checkbox,
        ComponentState state,
        CancellationToken cancellationToken = default
    ) => Spec(
        context,
        state,
        cancellationToken,
        ("type", CHECKBOX_TYPE),
        ("id", checkbox.Id, Number),
        ("custom_id", checkbox.CustomId, String),
        ("default", checkbox.Default, Bool)
    );

    public Result<JsonNode> RenderCheckboxGroup(
        IRenderContext<JsonNode> context,
        CheckboxGroupComponentNode checkboxGroup,
        ComponentState state,
        CancellationToken cancellationToken = default
    ) => Spec(
        context,
        state,
        cancellationToken,
        ("type", CHECKBOX_GROUP_TYPE),
        ("id", checkboxGroup.Id, Number),
        ("custom_id", checkboxGroup.CustomId, String),
        ("options", checkboxGroup.Options, ComponentArray),
        ("min_values", checkboxGroup.MinValues, Number),
        ("max_values", checkboxGroup.MaxValues, Number),
        ("required", checkboxGroup.Required, Bool)
    );

    public Result<JsonNode> RenderCheckboxGroupOption(
        IRenderContext<JsonNode> context,
        CheckboxGroupOptionComponentNode checkboxGroupOption,
        ComponentState state,
        CancellationToken cancellationToken = default
    ) => Spec(
        context,
        state,
        cancellationToken,
        ("value", checkboxGroupOption.Value, String),
        ("label", checkboxGroupOption.Label, String),
        ("description", checkboxGroupOption.Description, String),
        ("default", checkboxGroupOption.Default, Bool)
    );
}