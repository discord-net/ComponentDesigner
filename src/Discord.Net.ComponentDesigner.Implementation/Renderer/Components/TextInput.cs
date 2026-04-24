using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    public static Result<CSharpRender> RenderTextInput(
        IRenderContext<CSharpRender> context,
        TextInputComponentNode textInput,
        ComponentState state,
        CancellationToken cancellationToken
    ) => Construct(
        context,
        state,
        context.CompilationProvider.TextInputBuilder,
        cancellationToken,
        ("id", textInput.Id, CSharpValueGenerator.NullableInt32),
        ("customId", textInput.CustomId, CSharpValueGenerator.String),
        ("style", textInput.Style, CSharpValueGenerator.TextInputStyle),
        ("minLength", textInput.MinLength, CSharpValueGenerator.NullableInt32),
        ("maxLength", textInput.MaxLength, CSharpValueGenerator.NullableInt32),
        ("required", textInput.Required, CSharpValueGenerator.NullableBoolean),
        ("value", textInput.Value, CSharpValueGenerator.NullableString),
        ("placeholder", textInput.Placeholder, CSharpValueGenerator.NullableString)
    );
}