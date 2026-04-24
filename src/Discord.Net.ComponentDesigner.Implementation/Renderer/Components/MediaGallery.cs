using System.Diagnostics.CodeAnalysis;
using System.Text;
using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace Discord;

partial class DiscordNetRenderer
{
    private static readonly CSharpValueTransformer MediaGalleryItems
        = CollectionOf(Symbols.MediaGalleryItemProperties, MediaGalleryItemConverter);

    public static Result<CSharpRender> RenderMediaGallery(
        IRenderContext<CSharpRender> context,
        MediaGalleryComponentNode mediaGallery,
        ComponentState state,
        CancellationToken cancellationToken
    ) => Construct(
        context,
        state,
        context.CompilationProvider.MediaGalleryBuilder,
        cancellationToken,
        ("id", mediaGallery.Id, CSharpValueGenerator.NullableInt32),
        ("items", mediaGallery.Items, MediaGalleryItems)
    );

    private static Result<CSharpRender> MediaGalleryItemConverter(
        IRenderContext<CSharpRender> context,
        CSharpRender render,
        ICSharpTypeSymbol target,
        Converter next,
        CancellationToken cancellationToken
    )
    {
        if (render.Symbol is not null)
        {
            var isEnumerable = render.Symbol.TryGetEnumerableType(out var sourceSymbol);
            sourceSymbol ??= render.Symbol;

            if (TryGetConverter(context, sourceSymbol, cancellationToken, out var converter))
            {
                Func<string, string> mapper = isEnumerable
                    ? x => $"{x}.Select(x => {converter("x")})"
                    : converter;

                return render with
                {
                    Source = mapper(render.Source),
                    Symbol = target
                };
            }
        }
        
        return next(context, render, target, cancellationToken);

        static bool TryGetConverter(
            IRenderContext context,
            ICSharpTypeSymbol symbol,
            CancellationToken cancellationToken,
            [MaybeNullWhen(false)] out Func<string, string> converter
        )
        {
            if (symbol.Equals(context.CompilationProvider.String, cancellationToken))
            {
                converter = ConvertString;
                return true;
            }

            if (symbol.Equals(context.CompilationProvider.SystemUri, cancellationToken))
            {
                converter = ConvertUri;
                return true;
            }

            converter = null;
            return false;
        }
        
        static string ConvertString(string str)
            => $"""
                new global::Discord.MediaGalleryItemProperties(
                    media: new global::Discord.UnfurledMediaItemProperties(
                        {str}
                    )
                )
                """;
        
        static string ConvertUri(string uri)
            => $"""
                new global::Discord.MediaGalleryItemProperties(
                    media: new global::Discord.UnfurledMediaItemProperties(
                        {uri}.ToString()
                    )
                )
                """;
    }

    public static Result<CSharpRender> RenderMediaGalleryItem(
        IRenderContext<CSharpRender> context,
        MediaGalleryItemComponentNode mediaGalleryItem,
        ComponentState state,
        CancellationToken cancellationToken
    ) => Construct(
        context,
        state,
        context.CompilationProvider.MediaGalleryItemProperties,
        cancellationToken,
        ("media", mediaGalleryItem.Media, CSharpValueGenerator.UnfurledMediaItemProperties),
        ("description", mediaGalleryItem.Description, CSharpValueGenerator.NullableString),
        ("isSpoiler", mediaGalleryItem.Spoiler, CSharpValueGenerator.NullableBoolean)
    );
}