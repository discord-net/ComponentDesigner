using ComponentDesigner;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    public override Result<RenderedComponent> RenderActionRow(
        IRendererContext context,
        ActionRowComponentNode actionRow,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => context.CompilationProvider
        .ActionRowBuilder(state.TextSpan, cancellationToken)
        .Combine(
            RenderPropertiesAsParameters(
                context, state, cancellationToken,
                ("id", actionRow.Id, CSharpValueGenerator.NullableInteger),
                ("components", actionRow.Components, new(RenderActionRowComponents))
            ),
            (symbol, parameters) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({parameters})",
                symbol
            )
        );

    private static Result<string> RenderActionRowComponents(
        IRendererContext context,
        ComponentPropertyValue value,
        CancellationToken cancellationToken
    ) => context
        .CompilationProvider
        .IEnumerableOfIMessageComponentBuilder(value, cancellationToken)
        .Map(symbol => RenderAsChildComponents(context, value, symbol, cancellationToken, true));
}