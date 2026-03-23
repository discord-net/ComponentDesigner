using ComponentDesigner;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    public override Result<RenderedComponent> RenderButton(
        IRendererContext context,
        ButtonComponentNode button,
        ButtonState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    )
    {
        var styleRenderer = state.InferredKind is not null and not ButtonKind.Default
            ? new Result<PropertyRenderer>(new PropertyRenderer($"global::Discord.ButtonStyle.{state.InferredKind}"))
            : CSharpValueGenerator
                .ButtonStyle(context.CompilationProvider, state.TextSpan, cancellationToken)
                .Map(x => new PropertyRenderer(x));

        return context.CompilationProvider
            .ButtonBuilder(state.TextSpan, cancellationToken)
            .Combine(
                styleRenderer
                    .Map(styleRenderer =>
                        RenderPropertiesAsParameters(
                            context, state, cancellationToken,
                            ("id", button.Id, CSharpValueGenerator.NullableInt32),
                            ("style", button.Style, styleRenderer),
                            ("label", button.Label, CSharpValueGenerator.NullableString),
                            ("emoji", button.Emoji, CSharpValueGenerator.NullableEmoji),
                            ("customId", button.CustomId, CSharpValueGenerator.String),
                            ("skuId", button.SkuId, CSharpValueGenerator.NullableUInt64),
                            ("url", button.Url, CSharpValueGenerator.NullableString),
                            ("isDisabled", button.Disabled, CSharpValueGenerator.Boolean)
                        )
                    ),
                (symbol, parameters) => new RenderedComponent(
                    $"new {symbol.ToQualifiedName()}({parameters})",
                    symbol
                )
            )
            .Map(GetConverterFromOptions(context, state, typingContext, cancellationToken));
    }
}