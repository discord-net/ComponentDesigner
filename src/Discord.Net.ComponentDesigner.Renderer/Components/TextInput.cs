using ComponentDesigner;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    public override Result<RenderedComponent> RenderTextInput(
        IRendererContext context,
        TextInputComponentNode textInput,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => context.CompilationProvider
        .TextInputBuilder(state.TextSpan, cancellationToken)
        .Combine(
            RenderPropertiesAsParameters(
                context, state, cancellationToken,
                ("id", textInput.Id, CSharpValueGenerator.NullableInteger),
                ("customId", textInput.CustomId, CSharpValueGenerator.String),
                ("style", textInput.Style,
                    CSharpValueGenerator.TextInputStyle(
                        context.CompilationProvider,
                        state.TextSpan,
                        cancellationToken
                    )
                ),
                ("minLength", textInput.MinLength, CSharpValueGenerator.NullableInteger),
                ("maxLength", textInput.MaxLength, CSharpValueGenerator.NullableInteger),
                ("required", textInput.Required, CSharpValueGenerator.NullableBoolean),
                ("value", textInput.Value, CSharpValueGenerator.NullableString),
                ("placeholder", textInput.Placeholder, CSharpValueGenerator.NullableString)
            ),
            (symbol, parameters) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({parameters})"
            )
        );
}