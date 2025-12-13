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
        // Store Uri interpolations with their position index in the source children
        public List<(int ChildIndex, int InterpolationIndex, DesignerInterpolationInfo Info)> UriInterpolations { get; } = [];
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

        var state = new MediaGalleryState { Source = element };

        // Add children for normal processing (media-gallery-item elements)
        context.AddChildren(element.Children);

        // Extract Uri interpolations from children for later processing, tracking their position
        for (int i = 0; i < element.Children.Count; i++)
        {
            ExtractUriInterpolations(element.Children[i], i, state, context);
        }

        return state;
    }

    private void ExtractUriInterpolations(CXNode node, int childIndex, MediaGalleryState state, ComponentStateInitializationContext context)
    {
        // Note: We can't access IComponentContext here, so we defer the actual type checking to validation/rendering
        // For now, just mark potential interpolations with their position
        if (node is CXValue.Interpolation interpolation)
        {
            state.UriInterpolations.Add((childIndex, interpolation.InterpolationIndex, default!));
        }
        else if (node is CXValue.Multipart multipart)
        {
            foreach (var token in multipart.Tokens)
            {
                if (token.InterpolationIndex is { } index)
                {
                    state.UriInterpolations.Add((childIndex, index, default!));
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

        // Update and count Uri interpolations from state
        for (int i = 0; i < state.UriInterpolations.Count; i++)
        {
            var (childIndex, index, _) = state.UriInterpolations[i];
            var info = context.GetInterpolationInfo(index);
            
            // Update the info in the state for later use in rendering
            state.UriInterpolations[i] = (childIndex, index, info);
            
            if (IsUriType(info.Symbol, context.Compilation))
            {
                validItemCount++;
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
                // If Uri interpolations caused the overflow, report on the whole gallery
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
                // Check if this is a Uri interpolation
                var uriInterpolations = state.UriInterpolations
                    .Where(x => x.ChildIndex == i)
                    .ToList();
                
                foreach (var (_, index, info) in uriInterpolations)
                {
                    if (IsUriType(info.Symbol, context.Compilation))
                    {
                        results.Add(RenderMediaGalleryItemForUri(context, index, info));
                    }
                }
            }
        }

        return results
            .FlattenAll()
            .Map(x => string.Join($",{Environment.NewLine}", x));
    }

    private string RenderMediaGalleryItemForUri(IComponentContext context, int interpolationIndex, DesignerInterpolationInfo info)
    {
        var renderedUri = context.GetDesignerValue(
            interpolationIndex,
            info.Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        );
        
        return $"""
            new {context.KnownTypes.MediaGalleryItemPropertiesType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}(
                media: new {context.KnownTypes.UnfurledMediaItemPropertiesType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({renderedUri})
            )
            """;
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