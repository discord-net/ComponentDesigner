using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    public static Result<CSharpRender> RenderCheckbox(
        IRenderContext<CSharpRender> context,
        CheckboxComponentNode checkbox,
        ComponentState state,
        CancellationToken cancellationToken
    ) => Construct(
        context,
        state,
        context.CompilationProvider.CheckboxBuilder,
        cancellationToken,
        ("customId", checkbox.CustomId, CSharpValueGenerator.String),
        ("defaultState", checkbox.Default, CSharpValueGenerator.NullableBoolean),
        ("id", checkbox.Id, CSharpValueGenerator.NullableInt32)
    );

    private static readonly CSharpValueTransformer CheckboxGroupOptions
        = CollectionOf(Symbols.CheckboxGroupOptionProperties);
    
    public static Result<CSharpRender> RenderCheckboxGroup(
        IRenderContext<CSharpRender> context,
        CheckboxGroupComponentNode checkboxGroup,
        ComponentState state,
        CancellationToken cancellationToken
    ) => Construct(
        context,
        state,
        context.CompilationProvider.CheckboxGroupBuilder,
        cancellationToken,
        ("customId", checkboxGroup.CustomId, CSharpValueGenerator.String),
        ("options", checkboxGroup.Options, CheckboxGroupOptions),
        ("minValues", checkboxGroup.MinValues, CSharpValueGenerator.NullableInt32),
        ("maxValues", checkboxGroup.MaxValues, CSharpValueGenerator.NullableInt32),
        ("isRequired", checkboxGroup.Required, CSharpValueGenerator.NullableBoolean),
        ("id", checkboxGroup.Id, CSharpValueGenerator.NullableInt32)
    );
    
    public static Result<CSharpRender> RenderCheckboxGroupOption(
        IRenderContext<CSharpRender> context,
        CheckboxGroupOptionComponentNode checkboxGroupOption,
        ComponentState state,
        CancellationToken cancellationToken
    ) => Construct(
        context,
        state,
        context.CompilationProvider.CheckboxGroupOptionProperties,
        cancellationToken,
        ("value", checkboxGroupOption.Value, CSharpValueGenerator.String),
        ("label", checkboxGroupOption.Label, CSharpValueGenerator.String),
        ("description", checkboxGroupOption.Description, CSharpValueGenerator.NullableString),
        ("defaultState", checkboxGroupOption.Default, CSharpValueGenerator.NullableBoolean)
    );
}