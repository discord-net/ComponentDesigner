using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

using InterpolationIndex = int;

public sealed class MediaGalleryComponentNode : ComponentNode<MediaGalleryComponentNode.MediaGalleryState>
{
    public sealed class MediaGalleryState : ComponentState
    {
        // Store interpolations with their position index in the source children
        // Info is not stored - it's retrieved from context during Validate/Render
        public required EquatableArray<InterpolationIndex> Interpolations { get; init; }
    }

    public override string Name => "media-gallery";

    public override IReadOnlyList<string> Aliases { get; } = ["gallery"];

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public override bool HasChildren => true;

    public MediaGalleryComponentNode()
    {
        Properties =
        [
            ComponentProperty.Id,
        ];
    }

    public override MediaGalleryState? CreateState(ComponentStateInitializationContext context)
    {
        if (context.Node is not CXElement element) return null;

        // Add children for normal processing (media-gallery-item elements)
        context.AddChildren(element.Children);

        // Extract interpolations from children for later processing, tracking their position
        var interpolations = new List<InterpolationIndex>();
        for (var i = 0; i < element.Children.Count; i++)
        {
            ExtractInterpolations(element.Children[i], i, interpolations);
        }

        return new MediaGalleryState
        {
            Source = element,
            Interpolations = [..interpolations]
        };
    }

    private void ExtractInterpolations(CXNode node, int childIndex, List<InterpolationIndex> interpolations)
    {
        // Extract all interpolations regardless of type - type checking happens during validation/rendering
        switch (node)
        {
            case CXValue.Interpolation interpolation:
                interpolations.Add(interpolation.InterpolationIndex);
                break;
            case CXValue.Multipart { HasInterpolations: true } multipart:
                foreach (var token in multipart.Tokens)
                {
                    if (token.InterpolationIndex is null) continue;
                    interpolations.Add(token.InterpolationIndex.Value);
                }

                break;
        }
    }

    private static bool IsUriType(ITypeSymbol? symbol, Compilation compilation)
    {
        if (symbol is null) return false;

        var knownTypes = compilation.GetKnownTypes();
        var uriType = knownTypes.UriType;
        if (uriType is null)
        {
            // Fallback: Check if the symbol's fully qualified name is System.Uri
            var fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return fullName == "global::System.Uri";
        }

        return SymbolEqualityComparer.Default.Equals(symbol, uriType) ||
               compilation.HasImplicitConversion(symbol, uriType);
    }

    private static bool IsStringType(ITypeSymbol? symbol, Compilation compilation)
        => symbol?.SpecialType is SpecialType.System_String;

    private static bool IsUnfurledMediaItemType(ITypeSymbol? symbol, Compilation compilation)
    {
        if (symbol is null) return false;

        var knownTypes = compilation.GetKnownTypes();
        var unfurledType = knownTypes.UnfurledMediaItemPropertiesType;
        if (unfurledType is null) return false;

        return SymbolEqualityComparer.Default.Equals(symbol, unfurledType) ||
               compilation.HasImplicitConversion(symbol, unfurledType);
    }

    private static bool IsEnumerableOfSupportedType(ITypeSymbol? symbol, Compilation compilation,
        out ITypeSymbol? elementType)
    {
        if (!symbol.TryGetEnumerableType(out elementType))
        {
            elementType = null;
            return false;
        }

        // Check if T is one of the supported types
        return IsUriType(elementType, compilation) ||
               IsStringType(elementType, compilation) ||
               IsUnfurledMediaItemType(elementType, compilation);
    }

    [Flags]
    public enum InterpolationType
    {
        Unsupported = 0,
        Uri = 1,
        String = 2,
        UnfurledMediaItem = 4,
        EnumerableOf = 1 << 3,

        EnumerableOfUri = Uri | EnumerableOf,
        EnumerableOfString = String | EnumerableOf,
        EnumerableOfUnfurledMediaItem = UnfurledMediaItem | EnumerableOf
    }

    private static InterpolationType GetInterpolationType(ITypeSymbol? symbol, Compilation compilation)
    {
        if (symbol is null) return InterpolationType.Unsupported;

        var kind = InterpolationType.Unsupported;

        if (symbol.SpecialType is not SpecialType.System_String && symbol.TryGetEnumerableType(out var inner))
        {
            kind |= InterpolationType.EnumerableOf;
            symbol = inner;
        }

        if (IsUriType(symbol, compilation))
            kind |= InterpolationType.Uri;
        else if (IsStringType(symbol, compilation))
            kind |= InterpolationType.String;
        else if (IsUnfurledMediaItemType(symbol, compilation))
            kind |= InterpolationType.UnfurledMediaItem;
        else return InterpolationType.Unsupported;

        return kind;
    }

    private static bool IsValidChild(ComponentNode node)
        => node is IDynamicComponentNode
            or MediaGalleryItemComponentNode;

    public override void Validate(MediaGalleryState state, IComponentContext context, IList<DiagnosticInfo> diagnostics)
    {
        var validItemCount = 0;
        var hasEnumerables = false;

        // Count valid children from the graph
        foreach (var child in state.Children)
        {
            if (!IsValidChild(child.Inner))
            {
                diagnostics.Add(
                    Diagnostics.InvalidMediaGalleryChild(child.Inner.Name),
                    child.State.Source
                );
            }
            else validItemCount++;
        }

        // Count interpolations based on their type
        foreach (var index in state.Interpolations)
        {
            var info = context.GetInterpolationInfo(index);
            var interpType = GetInterpolationType(info.Symbol, context.Compilation);

            // Count items based on interpolation type
            if (interpType == InterpolationType.Unsupported)
            {
                // Report unsupported type as diagnostic
                var node = state.Source
                               .Document
                               ?.InterpolationTokens
                               .ElementAtOrDefault(index)
                           ?? state.Source;

                diagnostics.Add(
                    Diagnostics.InvalidMediaGalleryChild(info.Symbol?.ToDisplayString() ?? "unknown"),
                    node
                );
            }
            else if ((interpType & InterpolationType.EnumerableOf) != 0)
            {
                // For enumerables, assume they are empty for static validation (runtime check exists)
                // But track that we have them so we don't report empty gallery error
                hasEnumerables = true;
            }
            else
            {
                // Single item types
                validItemCount++;
            }
        }

        // Only report empty gallery if there are no items AND no enumerables
        if (validItemCount is 0 && !hasEnumerables)
        {
            diagnostics.Add(
                Diagnostics.MediaGalleryIsEmpty,
                state.Source
            );
        }
        else if (validItemCount > Constants.MAX_MEDIA_ITEMS)
        {
            // Report the error on items beyond the limit
            var graphValidChildren = state.Children.Where(x => IsValidChild(x.Inner)).ToArray();

            if (graphValidChildren.Length > Constants.MAX_MEDIA_ITEMS)
            {
                var extra = graphValidChildren.Skip(Constants.MAX_MEDIA_ITEMS).ToArray();
                var span = TextSpan.FromBounds(
                    extra[0].State.Source.Span.Start,
                    extra[extra.Length - 1].State.Source.Span.End
                );

                diagnostics.Add(
                    Diagnostics.TooManyItemsInMediaGallery,
                    span
                );
            }
            else
            {
                // If interpolations caused the overflow, report on the whole gallery
                diagnostics.Add(
                    Diagnostics.TooManyItemsInMediaGallery,
                    state.Source
                );
            }
        }

        base.Validate(state, context, diagnostics);
    }

    public override Result<string> Render(
        MediaGalleryState state,
        IComponentContext context,
        ComponentRenderingOptions options
    ) => state
        .RenderProperties(this, context, asInitializers: true)
        .Combine(RenderChildrenWithUriWrapping(state, context))
        .Map(x =>
        {
            var (props, children) = x;

            var init = new StringBuilder(props);

            if (!string.IsNullOrWhiteSpace(children))
            {
                if (!string.IsNullOrWhiteSpace(props)) init.Append(',').AppendLine();

                init.Append(
                    $"""
                     Items =
                     [
                         {children.WithNewlinePadding(4)}
                     ]
                     """
                );
            }

            return
                $"new {context.KnownTypes.MediaGalleryBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}(){
                    init.ToString()
                        .WithNewlinePadding(4)
                        .PrefixIfSome($"{Environment.NewLine}{{{Environment.NewLine}".Postfix(4))
                        .PostfixIfSome($"{Environment.NewLine}}}")}";
        });

    private Result<string> RenderChildrenWithUriWrapping(
        MediaGalleryState state,
        IComponentContext context
    ) => GetOrderedChildrenRenderers(state, context)
        .Select(renderer => renderer(state, context))
        .FlattenAll()
        .Map(x => string.Join($",{Environment.NewLine}", x));

    private static IEnumerable<ComponentNodeRenderer<MediaGalleryState>> GetOrderedChildrenRenderers(
        MediaGalleryState state,
        IComponentContext context
    )
    {
        if (state.Source is not CXElement element) yield break;

        var stack = new Stack<ICXNode>();

        // Push children in reverse order
        for (var i = element.Children.Count - 1; i >= 0; i--)
            stack.Push(element.Children[i]);

        var childPointer = 0;
        var interpPointer = 0;

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            switch (current)
            {
                case CXElement elem:
                    var child = state.Children.Count > childPointer
                        ? state.Children[childPointer++]
                        : null;

                    if (child is not null && ReferenceEquals(elem, child.State.Source))
                        yield return (_, ctx, _) => child.Render(ctx);

                    break;

                case CXValue.Multipart multi:
                    for (var i = multi.Tokens.Count - 1; i >= 0; i--)
                        stack.Push(multi.Tokens[i]);
                    break;

                case CXValue.Interpolation interp
                    when TryGetInterpolationRenderer(
                        interp.InterpolationIndex,
                        ref interpPointer,
                        state,
                        context,
                        out var renderer
                    ):
                    yield return renderer;
                    break;

                case CXToken { Kind: CXTokenKind.Interpolation, InterpolationIndex: { } idx }
                    when TryGetInterpolationRenderer(idx, ref interpPointer, state, context, out var renderer):
                    yield return renderer;
                    break;
            }
        }
    }

    private static bool TryGetInterpolationRenderer(
        int interpolationIndex,
        ref int pointer,
        MediaGalleryState state,
        IComponentContext context,
        out ComponentNodeRenderer<MediaGalleryState> renderer
    )
    {
        renderer = null!;

        if (pointer >= state.Interpolations.Count)
            return false;

        var stateIndex = state.Interpolations[pointer];

        if (stateIndex != interpolationIndex)
            return false;

        pointer++;

        var info = context.GetInterpolationInfo(interpolationIndex);
        var interpType = GetInterpolationType(info.Symbol, context.Compilation);

        if (interpType == InterpolationType.Unsupported)
            return false;

        renderer = (_, ctx, _) => RenderInterpolation(ctx, info, interpType);
        return true;
    }

    private static string RenderInterpolation(
        IComponentContext context,
        DesignerInterpolationInfo info,
        InterpolationType type
    )
    {
        var typeStr = info.Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var designerValue = context.GetDesignerValue(info, typeStr);

        var mediaGalleryItemType = context.KnownTypes.MediaGalleryItemPropertiesType!.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat
        );

        var unfurledMediaType =
            context.KnownTypes.UnfurledMediaItemPropertiesType!.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat);

        var isEnumerable = (type & InterpolationType.EnumerableOf) != 0;
        var baseType = type & ~InterpolationType.EnumerableOf;

        var source = isEnumerable ? "x" : designerValue;

        var renderer = baseType switch
        {
            InterpolationType.String => $"new {unfurledMediaType}({source})",
            InterpolationType.Uri => $"new {unfurledMediaType}({source}.ToString())",
            InterpolationType.UnfurledMediaItem => source,
            _ => throw new InvalidOperationException($"Unsupported interpolation type: {baseType}")
        };

        renderer
            = $"""
               new {mediaGalleryItemType}(
                   media: {renderer}
               )
               """;


        if (isEnumerable)
            renderer = $"..{designerValue}.Select(x => {renderer})";

        return renderer;
    }
}