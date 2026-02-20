using ComponentDesigner;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    public override Result<RenderedComponent> RenderFile(
        IRendererContext context,
        FileComponentNode file,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => context.CompilationProvider
        .FileBuilder(state.TextSpan, cancellationToken)
        .Combine(
            RenderPropertiesAsParameters(
                context, state, cancellationToken,
                ("id", file.Id, CSharpValueGenerator.NullableInteger),
                ("media", file.Media, CSharpValueGenerator.UnfurledMediaItemProperties),
                ("isSpoiler", file.IsSpoiler, CSharpValueGenerator.Boolean)
            ),
            (symbol, parameters) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({parameters})",
                symbol
            )
        );
}