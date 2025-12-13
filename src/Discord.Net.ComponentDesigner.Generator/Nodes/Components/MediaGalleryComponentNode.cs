using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

public sealed class MediaGalleryComponentNode : ComponentNode
{
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

    private static bool IsUriType(ITypeSymbol? symbol, Compilation compilation)
    {
        if (symbol is null) return false;
        
        // Check if the symbol's fully qualified name is System.Uri
        var fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (fullName == "global::System.Uri")
            return true;
        
        var uriType = compilation.GetTypeByMetadataName("System.Uri");
        if (uriType is null) return false;

        return SymbolEqualityComparer.Default.Equals(symbol, uriType) ||
               compilation.HasImplicitConversion(symbol, uriType);
    }

    public override void Validate(ComponentState state, IComponentContext context, IList<DiagnosticInfo> diagnostics)
    {
        var validItemCount = 0;
        
        // Count valid children from the graph
        foreach (var child in state.Children)
        {
            if (child.Inner is not (IDynamicComponentNode or MediaGalleryItemComponentNode))
            {
                diagnostics.Add(
                    Diagnostics.InvalidMediaGalleryChild(child.Inner.Name),
                    child.State.Source
                );
            }
            else validItemCount++;
        }

        // Also count valid Uri interpolations from source that didn't create graph nodes
        if (state.Source is CXElement element)
        {
            foreach (var sourceChild in element.Children)
            {
                validItemCount += CountUriInterpolations(sourceChild, context);
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
            var graphValidChildren = state.Children.Where(x => x.Inner is (IDynamicComponentNode or MediaGalleryItemComponentNode)).ToArray();
            
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

    private int CountUriInterpolations(CXNode node, IComponentContext context)
    {
        if (node is CXValue.Interpolation interpolation)
        {
            var info = context.GetInterpolationInfo(interpolation.InterpolationIndex);
            return IsUriType(info.Symbol, context.Compilation) ? 1 : 0;
        }
        else if (node is CXValue.Multipart multipart)
        {
            // Count each Uri interpolation in the multipart
            var count = 0;
            foreach (var token in multipart.Tokens)
            {
                if (token.InterpolationIndex is { } index)
                {
                    var info = context.GetInterpolationInfo(index);
                    if (IsUriType(info.Symbol, context.Compilation))
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        return 0;
    }

    public override Result<string> Render(
        ComponentState state,
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
        ComponentState state,
        IComponentContext context
    )
    {
        var results = new List<Result<string>>();

        // Render graph node children normally
        foreach (var child in state.Children)
        {
            results.Add(child.Render(context));
        }

        // Also process source children to find Uri interpolations that didn't create graph nodes
        if (state.Source is CXElement element)
        {
            foreach (var sourceChild in element.Children)
            {
                RenderUriInterpolations(sourceChild, context, results);
            }
        }

        return results
            .FlattenAll()
            .Map(x => string.Join($",{Environment.NewLine}", x));
    }

    private void RenderUriInterpolations(CXNode node, IComponentContext context, List<Result<string>> results)
    {
        if (node is CXValue.Interpolation interpolation)
        {
            var info = context.GetInterpolationInfo(interpolation.InterpolationIndex);
            if (IsUriType(info.Symbol, context.Compilation))
            {
                var renderedUri = context.GetDesignerValue(
                    interpolation.InterpolationIndex,
                    info.Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                );
                
                results.Add(
                    $"""
                    new {context.KnownTypes.MediaGalleryItemPropertiesType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}(
                        media: new {context.KnownTypes.UnfurledMediaItemPropertiesType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({renderedUri})
                    )
                    """
                );
            }
        }
        else if (node is CXValue.Multipart multipart)
        {
            // Render each Uri interpolation in the multipart
            foreach (var token in multipart.Tokens)
            {
                if (token.InterpolationIndex is { } index)
                {
                    var info = context.GetInterpolationInfo(index);
                    if (IsUriType(info.Symbol, context.Compilation))
                    {
                        var renderedUri = context.GetDesignerValue(
                            index,
                            info.Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                        );
                        
                        results.Add(
                            $"""
                            new {context.KnownTypes.MediaGalleryItemPropertiesType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}(
                                media: new {context.KnownTypes.UnfurledMediaItemPropertiesType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({renderedUri})
                            )
                            """
                        );
                    }
                }
            }
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