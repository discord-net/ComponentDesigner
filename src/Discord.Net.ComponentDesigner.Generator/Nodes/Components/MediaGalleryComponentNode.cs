using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

public sealed class MediaGalleryComponentNode : ComponentNode<MediaGalleryComponentNode.MediaGalleryState>
{
    public sealed class MediaGalleryState : ComponentState
    {
        // Store interpolations with their position index in the source children
        // Info is not stored - it's retrieved from context during Validate/Render
        public required EquatableArray<(int ChildIndex, int InterpolationIndex)> Interpolations { get; init; }
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
        var interpolations = new List<(int ChildIndex, int InterpolationIndex)>();
        for (int i = 0; i < element.Children.Count; i++)
        {
            ExtractInterpolations(element.Children[i], i, interpolations);
        }

        return new MediaGalleryState 
        { 
            Source = element,
            Interpolations = [..interpolations]
        };
    }

    private void ExtractInterpolations(CXNode node, int childIndex, List<(int ChildIndex, int InterpolationIndex)> interpolations)
    {
        // Extract all interpolations regardless of type - type checking happens during validation/rendering
        if (node is CXValue.Interpolation interpolation)
        {
            interpolations.Add((childIndex, interpolation.InterpolationIndex));
        }
        else if (node is CXValue.Multipart multipart)
        {
            foreach (var token in multipart.Tokens)
            {
                if (token.InterpolationIndex is { } index)
                {
                    interpolations.Add((childIndex, index));
                }
            }
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
    {
        if (symbol is null) return false;
        
        var knownTypes = compilation.GetKnownTypes();
        var stringType = knownTypes.StringType;
        
        return SymbolEqualityComparer.Default.Equals(symbol, stringType) ||
               compilation.HasImplicitConversion(symbol, stringType);
    }

    private static bool IsUnfurledMediaItemType(ITypeSymbol? symbol, Compilation compilation)
    {
        if (symbol is null) return false;
        
        var knownTypes = compilation.GetKnownTypes();
        var unfurledType = knownTypes.UnfurledMediaItemPropertiesType;
        if (unfurledType is null) return false;
        
        return SymbolEqualityComparer.Default.Equals(symbol, unfurledType) ||
               compilation.HasImplicitConversion(symbol, unfurledType);
    }

    private static bool IsEnumerableOfSupportedType(ITypeSymbol? symbol, Compilation compilation, out ITypeSymbol? elementType)
    {
        elementType = null;
        if (symbol is null) return false;

        // Check if the type implements IEnumerable<T>
        var enumerableType = symbol
            .AllInterfaces
            .FirstOrDefault(i => 
                i.IsGenericType && 
                i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);

        if (enumerableType is null) return false;

        elementType = enumerableType.TypeArguments.FirstOrDefault();
        if (elementType is null) return false;

        // Check if T is one of the supported types
        return IsUriType(elementType, compilation) ||
               IsStringType(elementType, compilation) ||
               IsUnfurledMediaItemType(elementType, compilation);
    }

    private enum InterpolationType
    {
        Uri,
        String,
        UnfurledMediaItem,
        EnumerableOfUri,
        EnumerableOfString,
        EnumerableOfUnfurledMediaItem,
        Unsupported
    }

    private static InterpolationType GetInterpolationType(ITypeSymbol? symbol, Compilation compilation)
    {
        if (symbol is null) return InterpolationType.Unsupported;

        if (IsEnumerableOfSupportedType(symbol, compilation, out var elementType))
        {
            if (IsUriType(elementType, compilation))
                return InterpolationType.EnumerableOfUri;
            if (IsStringType(elementType, compilation))
                return InterpolationType.EnumerableOfString;
            if (IsUnfurledMediaItemType(elementType, compilation))
                return InterpolationType.EnumerableOfUnfurledMediaItem;
        }

        if (IsUriType(symbol, compilation))
            return InterpolationType.Uri;
        if (IsStringType(symbol, compilation))
            return InterpolationType.String;
        if (IsUnfurledMediaItemType(symbol, compilation))
            return InterpolationType.UnfurledMediaItem;

        return InterpolationType.Unsupported;
    }

    private static bool IsValidChild(ComponentNode node)
        => node is IDynamicComponentNode
            or MediaGalleryItemComponentNode;

    public override void Validate(MediaGalleryState state, IComponentContext context, IList<DiagnosticInfo> diagnostics)
    {
        var validItemCount = 0;
        
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
        foreach (var (childIndex, index) in state.Interpolations)
        {
            var info = context.GetInterpolationInfo(index);
            var interpType = GetInterpolationType(info.Symbol, context.Compilation);
            
            // Count items based on interpolation type
            switch (interpType)
            {
                case InterpolationType.Uri:
                case InterpolationType.String:
                case InterpolationType.UnfurledMediaItem:
                    validItemCount++;
                    break;
                case InterpolationType.EnumerableOfUri:
                case InterpolationType.EnumerableOfString:
                case InterpolationType.EnumerableOfUnfurledMediaItem:
                    // For enumerables, we can't know the count at compile time
                    // So we count it as 1 for validation purposes (could be 0 or more at runtime)
                    validItemCount++;
                    break;
                case InterpolationType.Unsupported:
                    // Unsupported type - will be ignored during rendering
                    break;
            }
        }

        if (validItemCount is 0)
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
    )
    {
        if (state.Source is not CXElement element) 
            return string.Empty;

        var results = new List<Result<string>>();
        var graphChildIndex = 0;

        // Render items in source order
        for (int i = 0; i < element.Children.Count; i++)
        {
            var sourceChild = element.Children[i];
            
            // Check if this source child created a graph node
            var isGraphChild = sourceChild is CXElement;
            
            if (isGraphChild && graphChildIndex < state.Children.Count)
            {
                // Render the graph child
                results.Add(state.Children[graphChildIndex].Render(context));
                graphChildIndex++;
            }
            else
            {
                // Check if there are interpolations at this position
                var interpolationsAtPosition = state.Interpolations
                    .Where(x => x.ChildIndex == i)
                    .ToList();
                
                foreach (var (_, index) in interpolationsAtPosition)
                {
                    var info = context.GetInterpolationInfo(index);
                    var interpType = GetInterpolationType(info.Symbol, context.Compilation);
                    
                    if (interpType != InterpolationType.Unsupported)
                    {
                        results.Add(RenderInterpolation(context, index, info, interpType));
                    }
                }
            }
        }

        return results
            .FlattenAll()
            .Map(x => string.Join($",{Environment.NewLine}", x));
    }

    private string RenderInterpolation(IComponentContext context, int interpolationIndex, DesignerInterpolationInfo info, InterpolationType type)
    {
        var typeStr = info.Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var designerValue = context.GetDesignerValue(interpolationIndex, typeStr);

        var mediaGalleryItemType = context.KnownTypes.MediaGalleryItemPropertiesType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var unfurledMediaType = context.KnownTypes.UnfurledMediaItemPropertiesType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        switch (type)
        {
            case InterpolationType.Uri:
                return $"""
                    new {mediaGalleryItemType}(
                        media: new {unfurledMediaType}({designerValue})
                    )
                    """;

            case InterpolationType.String:
                return $"""
                    new {mediaGalleryItemType}(
                        media: new {unfurledMediaType}({designerValue})
                    )
                    """;

            case InterpolationType.UnfurledMediaItem:
                return $"""
                    new {mediaGalleryItemType}(
                        media: {designerValue}
                    )
                    """;

            case InterpolationType.EnumerableOfUri:
            case InterpolationType.EnumerableOfString:
                // For enumerables of Uri or string, we need to map each element to UnfurledMediaItemProperties
                return $"""
                    ..{designerValue}.Select(x => new {mediaGalleryItemType}(
                        media: new {unfurledMediaType}(x)
                    ))
                    """;

            case InterpolationType.EnumerableOfUnfurledMediaItem:
                // For enumerables of UnfurledMediaItem, we can use them directly
                return $"""
                    ..{designerValue}.Select(x => new {mediaGalleryItemType}(
                        media: x
                    ))
                    """;

            default:
                return string.Empty;
        }
    }
}

public sealed class MediaGalleryItemComponentNode : ComponentNode
{
    public override string Name => "media-gallery-item";

    public override IReadOnlyList<string> Aliases { get; } = ["gallery-item", "media", "item"];

    public ComponentProperty Url { get; }
    public ComponentProperty Description { get; }
    public ComponentProperty Spoiler { get; }

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public MediaGalleryItemComponentNode()
    {
        Properties =
        [
            Url = new(
                "url",
                aliases: ["media"],
                renderer: Renderers.UnfurledMediaItem,
                dotnetParameterName: "media"
            ),
            Description = new(
                "description",
                isOptional: true,
                renderer: Renderers.String
            ),
            Spoiler = new(
                "spoiler",
                isOptional: true,
                renderer: Renderers.Boolean,
                dotnetParameterName: "isSpoiler"
            )
        ];
    }

    public override Result<string> Render(
        ComponentState state,
        IComponentContext context,
        ComponentRenderingOptions options
    ) => state
        .RenderProperties(this, context)
        .Map(x =>
            $"""
             new {context.KnownTypes.MediaGalleryItemPropertiesType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}(
                 {x.WithNewlinePadding(4)}
             )
             """
        );
}