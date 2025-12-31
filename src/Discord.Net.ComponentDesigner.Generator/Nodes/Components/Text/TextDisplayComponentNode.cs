using System;
using Discord.CX.Parser;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

public sealed record TextDisplayState(
    GraphNode GraphNode,
    ICXNode Source,
    TextControlElement? Content
) : ComponentState(GraphNode, Source);

public class TextDisplayComponentNode : ComponentNode<TextDisplayState>
{
    public override string Name => "text-display";

    public override IReadOnlyList<string> Aliases { get; } = ["text"];

    public ComponentProperty Content { get; }

    public override ImmutableArray<ComponentProperty> Properties { get; }

    protected override bool AllowChildrenInCX => true;

    public TextDisplayComponentNode()
    {
        Properties =
        [
            ComponentProperty.Id,
            Content = new(
                "content",
                renderer: CXValueGenerator.String,
                isOptional: true
            )
        ];
    }

    public override void AddGraphNode(ComponentGraphInitializationContext context)
    {
        context.Push(
            this,
            cxNode: context.CXNode
        );
    }

    public override TextDisplayState? CreateState(
        ComponentStateInitializationContext context,
        IList<DiagnosticInfo> diagnostics
    )
    {
        TextControlElement? content = null;

        if (context.CXNode is CXElement element)
        {
            TextControlElement.TryCreate(
                context.GraphContext,
                element.Children,
                diagnostics,
                out content,
                out var nodesUsed
            );

            context.AddChildren(element.Children.Skip(nodesUsed));
        }

        return new TextDisplayState(
            context.GraphNode,
            context.CXNode,
            content
        );
    }

    public override void Validate(TextDisplayState state, IComponentContext context, IList<DiagnosticInfo> diagnostics)
    {
        // check for content either as an attribute or as text controls
        var contentProperty = state.GetProperty(Content);

        if (state.Content is null && !contentProperty.HasValue)
        {
            diagnostics.Add(
                Diagnostics.MissingRequiredProperty(Name, contentProperty.PropertyName),
                contentProperty.Span
            );
        }
        
        base.Validate(state, context, diagnostics);
    }

    public override Result<string> Render(
        TextDisplayState state,
        IComponentContext context,
        ComponentRenderingOptions options
    ) => state
        .RenderProperties(
            this,
            context,
            ignorePredicate: state.Content is not null
                ? x => ReferenceEquals(x, Content)
                : null
        )
        .Combine(
            state.Content?.RenderToCSharpString(context) ?? string.Empty,
            (properties, content) =>
            {
                var props = properties;
                
                if (!string.IsNullOrEmpty(content))
                {
                    if (!string.IsNullOrEmpty(props))
                        props += $",{Environment.NewLine}";

                    props += $"{Content.DotnetParameterName}: {content}";
                }

                return
                    $"""
                     new {context.KnownTypes.TextDisplayBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({
                            props.PrefixIfSome(4)
                                .WithNewlinePadding(4)
                                .WrapIfSome(Environment.NewLine)
                        })
                     """;
            }
        )
        .Map(state.ConformResult(ComponentBuilderKind.IMessageComponentBuilder, options.TypingContext));
}