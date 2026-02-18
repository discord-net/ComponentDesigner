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
        );

    public override Result<RenderedComponent> RenderMediaGallery(
        IRendererContext context,
        MediaGalleryComponentNode mediaGallery,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    )
    {
        return context.CompilationProvider
            .MediaGalleryBuilder(state.TextSpan, cancellationToken)
            .Combine(
                RenderPropertiesAsParameters(
                    context, state, cancellationToken,
                    ("id", mediaGallery.Id, CSharpValueGenerator.NullableInteger),
                    ("items", mediaGallery.Items, new(RenderMediaGalleryItemsProperty))
                ),
                (symbol, properties) => new RenderedComponent(
                    $"new {symbol.ToQualifiedName()}({properties})",
                    symbol
                )
            );

        Result<string> RenderMediaGalleryItemsProperty(
            IRendererContext context,
            ComponentPropertyValue value,
            CancellationToken cancellationToken
        )
        {
            var results = new List<Result<string>>();
            var itemsCount = 0;

            if (value is ComponentPropertyValue.Many many)
            {
                foreach (var subValue in many.Values)
                {
                    RenderFlattenedValue(results, subValue);
                }
            }
            else
            {
                RenderFlattenedValue(results, value);
            }

            if (itemsCount > Validators.MEDIA_GALLERY_MAX_ITEMS)
                return Diagnostic.TooManyChildren(
                    mediaGallery,
                    Validators.MEDIA_GALLERY_MAX_ITEMS
                ).At(value.TextSpan);

            switch (results.Count)
            {
                case 0:
                    return Diagnostic
                        .RequiredPropertyNotSpecified(mediaGallery, mediaGallery.Items)
                        .At(state.ElementIdentifierTextSpanOrBetter);

                case 1: return results[0];

                default:
                    return results
                        .FlattenAll()
                        .Map(x =>
                            $"""

                             [
                                 {
                                     string.Join(
                                         $",{Environment.NewLine}".Postfix(4),
                                         x.Select(x => x
                                             .Map(x => x.WithNewlinePadding(4))
                                         )
                                     )
                                 }
                             ]
                             """
                        );
            }

            void RenderFlattenedValue(
                List<Result<string>> results,
                ComponentPropertyValue value
            )
            {
                switch (value)
                {
                    case ComponentPropertyValue.SyntaxValue syntax:
                        switch (syntax.CXValue)
                        {
                            case CXValue.Multipart multipart:
                                results.AddRange(multipart.Tokens.Select(FromFlattenedSyntax));
                                break;
                            case CXValue.Interpolation interpolation:
                                results.Add(FromFlattenedSyntax(interpolation.Token));
                                break;
                            default:
                                results.Add(
                                    Diagnostic
                                        .InvalidChildOfComponent(mediaGallery, syntax.CXValue)
                                        .At(syntax.CXValue)
                                );
                                break;
                        }

                        break;

                    case ComponentPropertyValue.Component children:
                        results.Add(RenderAsChildComponents(
                            context,
                            children,
                            cancellationToken,
                            withinCollectionExpression: false)
                        );
                        
                        if (children.GraphNode.Component is MediaGalleryItemComponentNode)
                            itemsCount++;
                        
                        break;

                    default:
                        results.Add(
                            Diagnostic
                                .ValueVariantCannotBeGenerated(value)
                                .At(value.TextSpan)
                        );
                        break;
                }
            }

            Result<string> FromFlattenedSyntax(CXToken token)
            {
                if (token.Kind is not CXTokenKind.Interpolation)
                    return Diagnostic.InvalidChildOfComponent(mediaGallery, token).At(token);

                var info = context.GetInterpolationInfo(token);

                var uri = context
                    .CompilationProvider
                    .SystemUri(default, cancellationToken)
                    .GetValueOrDefault();

                var mediaGalleryItemProperties = context
                    .CompilationProvider
                    .MediaGalleryItemProperties(default, cancellationToken)
                    .GetValueOrDefault();

                if (
                    uri is not null &&
                    uri.Equals(info.Symbol)
                )
                {
                    itemsCount++;
                    return $"new global::Discord.MediaGalleryItemProperties({
                        context.GetReferenceToDesignerValue(info, uri)
                    }.ToString())";
                }

                if (
                    mediaGalleryItemProperties is not null &&
                    mediaGalleryItemProperties.Equals(info.Symbol)
                )
                {
                    itemsCount++;
                    return context.GetReferenceToDesignerValue(info, mediaGalleryItemProperties);
                }

                return Diagnostic.TypeMismatch(
                    mediaGalleryItemProperties!,
                    info.Symbol!
                ).At(token);
            }
        }
    }
}