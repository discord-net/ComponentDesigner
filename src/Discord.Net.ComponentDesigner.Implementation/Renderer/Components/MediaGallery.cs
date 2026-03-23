using System.Diagnostics.CodeAnalysis;
using System.Text;
using ComponentDesigner;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace Discord;

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
        )
        .Map(GetConverterFromOptions(context, state, typingContext, cancellationToken));

    public override Result<RenderedComponent> RenderMediaGallery(
        IRendererContext context,
        MediaGalleryComponentNode mediaGallery,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => context.CompilationProvider
        .MediaGalleryBuilder(state.TextSpan, cancellationToken)
        .Combine(
            RenderPropertiesAsParameters(
                context, state, cancellationToken,
                ("id", mediaGallery.Id, CSharpValueGenerator.NullableInt32),
                ("items", mediaGallery.Items, new(RenderMediaGalleryItemsProperty))
            ),
            (symbol, properties) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({properties})",
                symbol
            )
        )
        .Map(GetConverterFromOptions(context, state, typingContext, cancellationToken));

    private delegate string MediaGalleryItemMapper(string source);

    private static Result<string> RenderMediaGalleryItemsProperty(
        IRendererContext context,
        ComponentPropertyValue value,
        CancellationToken cancellationToken
    )
    {
        using var bag = PooledDiagnosticBag.Get();
        using var _ = StringBuilder.Pooled(out var sb);

        foreach (var itemValue in value.AsFlattened)
        {
            var result = RenderSingleItem(context, itemValue, cancellationToken).Unwrap(bag);

            if (result is null) continue;

            if (sb.Length > 0) sb.AppendLine(",");

            sb.Append(result);
        }

        if (sb.Length is 0) return "[]";

        return
            $"""

             [
                 {sb.ToString().WithNewlinePadding(4)}
             ]
             """;


        static Result<string> RenderSingleItem(
            IRendererContext context,
            ComponentPropertyValue itemValue,
            CancellationToken cancellationToken
        )
        {
            switch (itemValue)
            {
                case ComponentPropertyValue.Component component:
                    return context
                        .RenderGraphNode(
                            component.GraphNode,
                            cancellationToken: cancellationToken
                        )
                        .AsSource;

                case ComponentPropertyValue.Interpolation interpolation:
                {
                    var targetSymbol = interpolation.Info.Symbol;
                    var isCollection = false;

                    if (targetSymbol is null)
                        return Diagnostic
                            .TypeMismatch(
                                "unknown",
                                "MediaGalleryItemProperties"
                            )
                            .At(itemValue);

                    if (
                        !targetSymbol.Equals(context.CompilationProvider.String!) &&
                        targetSymbol.TryGetEnumerableType(out var inner)
                    )
                    {
                        isCollection = true;
                        targetSymbol = inner;
                    }

                    if (!TryGetMapperForSymbol(context.CompilationProvider, targetSymbol, out var mapper,
                            cancellationToken))
                    {
                        return Diagnostic
                            .TypeMismatch(
                                "MediaGalleryItemProperties",
                                interpolation.Info.Symbol
                            )
                            .At(itemValue);
                    }

                    var finalMapper = isCollection
                        ? x => $"{x}.Select(x => {mapper("x")})"
                        : mapper;

                    return finalMapper(
                        context.GetReferenceToDesignerValue(interpolation.Info, interpolation.Info.Symbol)
                    );
                }

                default:
                    return Diagnostic
                        .InvalidPropertyValue(
                            itemValue,
                            ComponentPropertyValueKind.Component | ComponentPropertyValueKind.Interpolation
                        )
                        .At(itemValue);
            }
        }

        static bool TryGetMapperForSymbol(
            ICompilationProvider provider,
            ICSharpTypeSymbol symbol,
            [MaybeNullWhen(false)] out MediaGalleryItemMapper mapper,
            CancellationToken cancellationToken
        )
        {
            if (Is(provider.MediaGalleryItemProperties))
            {
                mapper = Identity;
                return true;
            }

            if (provider.String?.Equals(symbol) is true)
            {
                mapper = Literal;
                return true;
            }

            if (Is(provider.SystemUri))
            {
                mapper = Uri;
                return true;
            }

            mapper = null;
            return false;

            bool Is(Func<CXTextSpan, CancellationToken, Result<ICSharpTypeSymbol>> mapper)
            {
                var expected = mapper(default, cancellationToken).GetValueOrDefault();

                return expected is not null && expected.Equals(symbol);
            }

            static string Uri(string x)
                => $"""
                    new global::Discord.MediaGalleryItemProperties(
                        media: new global::Discord.UnfurledMediaItemProperties(
                            {x}.ToString()
                        )
                    )
                    """;

            static string Literal(string x)
                => $"""
                    new global::Discord.MediaGalleryItemProperties(
                        media: new global::Discord.UnfurledMediaItemProperties(
                            {x}
                        )
                    )
                    """;

            static string Identity(string x) => x;
        }
    }
}