using ComponentDesigner;
using ComponentDesigner.Nodes;

namespace Discord.ComponentDesigner;

partial class DiscordNetRenderer
{
    public override Result<RenderedComponent> RenderMediaGalleryItem(
        IRendererContext context,
        MediaGalleryItemComponentNode mediaGalleryItem,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => context.CompilationProvider
        .MediaGalleryItemProperties(state.TextSpan, cancellationToken)
        .Combine(
            RenderPropertiesAsParameters(
                context, state, cancellationToken,
                ("media", mediaGalleryItem.Media, CSharpValueGenerator.UnfurledMediaItemProperties),
                ("description", mediaGalleryItem.Description, CSharpValueGenerator.NullableString),
                ("isSpoiler", mediaGalleryItem.IsSpoiler, CSharpValueGenerator.Boolean)
            ),
            (symbol, properties) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({properties})",
                symbol
            )
        );

    public override Result<RenderedComponent> RenderMediaGallery(
        IRendererContext context,
        MediaGalleryComponentNode mediaGallery,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => context.CompilationProvider
        .MediaGalleryItemProperties(state.TextSpan, cancellationToken)
        .Combine(
            RenderPropertiesAsParameters(
                context, state, cancellationToken,
                ("id", mediaGallery.Id, CSharpValueGenerator.NullableInteger),
                ("items", mediaGallery.Items, new(RenderAsChildComponents))
            ),
            (symbol, properties) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({properties})",
                symbol
            )
        );
}