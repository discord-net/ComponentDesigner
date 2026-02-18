using ComponentDesigner;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    public override Result<RenderedComponent> RenderThumbnail(
        IRendererContext context,
        ThumbnailComponentNode thumbnail,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => context
        .CompilationProvider
        .ThumbnailBuilder(state.TextSpan, cancellationToken)
        .Combine(
            RenderPropertiesAsParameters(
                context, state, cancellationToken,
                ("id", thumbnail.Id, CSharpValueGenerator.NullableInteger),
                ("media", thumbnail.Media, CSharpValueGenerator.UnfurledMediaItemProperties),
                ("description", thumbnail.Description, CSharpValueGenerator.NullableString),
                ("isSpoiler", thumbnail.IsSpoiler, CSharpValueGenerator.Boolean)
            ),
            (symbol, parameters) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({parameters})",
                symbol
            )
        );
}