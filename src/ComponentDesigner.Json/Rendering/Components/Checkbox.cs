using System;
using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    public const int CHECKBOX_TYPE = 23;
    public const int CHECKBOX_GROUP_TYPE = 22;

    public Result<RenderedComponent> RenderCheckbox(
        IRendererContext context,
        CheckboxComponentNode checkbox,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => Build(
        context,
        state,
        cancellationToken,
        [("type", CHECKBOX_TYPE)],
        ("id", checkbox.Id, Number),
        ("custom_id", checkbox.CustomId, String),
        ("default", checkbox.Default, Bool)
    );

    public Result<RenderedComponent> RenderCheckboxGroup(
        IRendererContext context,
        CheckboxGroupComponentNode checkboxGroup,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => Build(
        context,
        state,
        cancellationToken,
        [("type", CHECKBOX_GROUP_TYPE)],
        ("id", checkboxGroup.Id, Number),
        ("custom_id", checkboxGroup.CustomId, String),
        ("options", checkboxGroup.Options, Components),
        ("min_values", checkboxGroup.MinValues, Number),
        ("max_values", checkboxGroup.MaxValues, Number),
        ("required", checkboxGroup.Required, Bool)
    );

    public Result<RenderedComponent> RenderCheckboxGroupOption(
        IRendererContext context,
        CheckboxGroupOptionComponentNode checkboxGroupOption,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => Build(
        context,
        state,
        cancellationToken,
        ("value", checkboxGroupOption.Value, String),
        ("label", checkboxGroupOption.Label, String),
        ("description", checkboxGroupOption.Description, String),
        ("default", checkboxGroupOption.Default, Bool)
    );
}