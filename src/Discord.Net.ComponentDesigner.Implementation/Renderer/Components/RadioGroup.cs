using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    private static readonly CSharpValueTransformer RadioGroupOptions
        = CollectionOf(Symbols.RadioGroupOptionProperties);

    public static Result<CSharpRender> RenderRadioGroup(
        IRenderContext<CSharpRender> context,
        RadioGroupComponentNode radioGroup,
        ComponentState state,
        CancellationToken cancellationToken
    ) => Construct(
        context,
        state,
        context.CompilationProvider.RadioGroupBuilder,
        cancellationToken,
        ("customId", radioGroup.CustomId, CSharpValueGenerator.String),
        ("options", radioGroup.Options, RadioGroupOptions),
        ("isRequired", radioGroup.Required, CSharpValueGenerator.NullableBoolean),
        ("id", radioGroup.Id, CSharpValueGenerator.NullableInt32)
    );
    
    public static Result<CSharpRender> RenderRadioGroupOption(
        IRenderContext<CSharpRender> context,
        RadioGroupOptionComponentNode radioGroupOption,
        ComponentState state,
        CancellationToken cancellationToken
    ) => Construct(
        context,
        state,
        context.CompilationProvider.RadioGroupBuilder,
        cancellationToken,
        ("value", radioGroupOption.Value, CSharpValueGenerator.String),
        ("label", radioGroupOption.Label, CSharpValueGenerator.String),
        ("description", radioGroupOption.Description, CSharpValueGenerator.NullableString),
        ("defaultState", radioGroupOption.Default, CSharpValueGenerator.NullableBoolean)
    );
}