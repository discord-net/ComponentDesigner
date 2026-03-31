using System.Diagnostics.CodeAnalysis;
using System.Text;
using ComponentDesigner;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;
using Discord.ComponentDesigner.LanguageServer.CX;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Discord.ComponentDesigner.LanguageServer;

public static class CompletionClassifier
{
    public static CompletionResult? Classify(
        CXComponentGraph graph,
        int position,
        ILogger logger
    )
    {
        var syntaxNode = SyntaxPosition.Get(graph.Document, Math.Max(0, position - 1));

        while (syntaxNode is not null)
        {
            logger.LogDebug("Classification at {Pos} in syntax node {Type}[{Span}]: {Val}", position,
                syntaxNode.GetType().Name, syntaxNode.TextSpan, syntaxNode);

            switch (syntaxNode)
            {
                case CXToken: break;

                case CXIdentifier { Parent: CXElement element } identifier:
                    return new CompletionResult.ComponentNames(graph, position, element, identifier.Value);

                case CXAttribute { Parent: ICXCollection { Parent: CXElement element } } attribute:

                    logger.LogDebug(
                        "Attribute identifier span: {Ident}\n" +
                        "Attribute has equals token?: {HasEq}\n" +
                        "Attribute has value?: {HasVal}",
                        attribute.IdentifierToken.TextSpan,
                        attribute.EqualsToken is not null,
                        attribute.Value is not null
                    );

                    // are we typing the attributes name?
                    if (
                        attribute.IdentifierToken.TextSpan.IntersectsWith(position) &&
                        attribute.EqualsToken is null
                    )
                    {
                        return new CompletionResult.ComponentAttributes(graph, position, element, attribute.Identifier);
                    }

                    if (
                        (attribute.Value is null && attribute.TextSpan.End <= position) ||
                        (attribute.Value is not null && attribute.Value.TextSpan.Contains(position))
                    )
                    {
                        return new CompletionResult.AttributeValue(graph, position, element, attribute);
                    }

                    break;

                case CXElement element:
                    if (
                        position >= element.OpeningTag.StartToken.TextSpan.End &&
                        position <= element.OpeningTag.EndToken.TextSpan.Start &&
                        (
                            element.OpeningTag.Identifier is null
                            ||
                            element.OpeningTag.Identifier.TextSpan.End >= position
                        )
                    )
                    {
                        // we're in the opening tag of the element, the attributes case didn't pass meaning we could
                        // only be looking for the name
                        return new CompletionResult.ComponentNames(graph, position, element, null);
                    }

                    /*
                     * we can check for the end of the identifier to be behind the position, any trivia (like spaces)
                     * should not count in the 'TextSpan' of the identifier
                     */
                    if (
                        element.OpeningTag.Identifier is not null &&
                        element.OpeningTag.Identifier.TextSpan.End < position &&
                        element.OpeningTag.EndToken.TextSpan.Start >= position
                    )
                    {
                        // we're in the attributes section
                        return new CompletionResult.ComponentAttributes(graph, position, element, null);
                    }

                    break;
            }

            syntaxNode = syntaxNode.Parent;
        }

        logger.LogDebug("Classification ends: syntax node is null");
        return null;
    }
}

/*
 * - Available component: <[...]|>
 * - Attributes: <foo |>
 */
public abstract record CompletionResult(CXComponentGraph Graph, int Position)
{
    public static readonly CompletionList EmptyCompletionList = new(isIncomplete: false, items: []);

    public abstract CompletionList ToCompletionList(ILogger logger);

    private bool TryGetGraphNode(CXElement element, [MaybeNullWhen(false)] out GraphNode graphNode)
        => Graph.TryLookupGraphNodeRepresentingSyntax(element, out graphNode);

    public sealed record AttributeValue(
        CXComponentGraph Graph,
        int Position,
        CXElement Element,
        CXAttribute Attribute
    ) : CompletionResult(Graph, Position)
    {
        public override CompletionList ToCompletionList(ILogger logger)
        {
            if (Attribute.EqualsToken is null) return EmptyCompletionList;

            if (!TryGetGraphNode(Element, out var graphNode)) return EmptyCompletionList;

            var property = graphNode
                .Component
                .Properties
                .FirstOrDefault(x => x.MatchesName(Attribute.Identifier));

            if (property is null) return EmptyCompletionList;

            var info = PropertyCompletionInfo.Get(graphNode.Component, property, graphNode.State);
            
            if (Attribute.Value is null)
            {
                return new CompletionList(
                    isIncomplete: false,
                    items:
                    [
                        new CompletionItem()
                        {
                            Label = property.Kind.ToString(),
                            Kind = CompletionItemKind.Value,
                            InsertTextFormat = info.HasAutoFill
                                ? InsertTextFormat.Snippet
                                : InsertTextFormat.PlainText,
                            TextEdit = new(new TextEdit()
                            {
                                NewText = info.AutoFill,
                                Range = ComponentDocument.GetRange(
                                    Graph.Document.Source!,
                                    Attribute.EqualsToken.FullTextSpan
                                )
                            })
                        }
                    ]
                );
            }

            // we're in an attribute value
            switch (Attribute.Value)
            {
                // TODO: choices
                
                // case CXValue.StringLiteral { HasInterpolations: false } literal when info.AutoFillKind is AutoFillKind.Choices:
                //     return new CompletionList(
                //         isIncomplete: false,
                //         items: property
                //             .AutoFillChoices
                //             .Select(x => new CompletionItem()
                //             {
                //                 InsertText = x,
                //                 Kind = CompletionItemKind.EnumMember,
                //                 Label = x,
                //                 SortText = literal.Tokens.ToValueString()
                //             })
                //     );
            }

            return EmptyCompletionList;
        }
    }

    public sealed record ComponentAttributes(
        CXComponentGraph Graph,
        int Position,
        CXElement Element,
        string? PartialIdentifier
    ) : CompletionResult(Graph, Position)
    {
        public override CompletionList ToCompletionList(ILogger logger)
        {
            if (!TryGetGraphNode(Element, out var graphNode))
            {
                logger.LogDebug("No graph node found for element");
                return EmptyCompletionList;
            }

            var items = new List<CompletionItem>();

            foreach (var property in graphNode.Component.Properties)
            {
                // don't suggest attributes already supplied
                if (Element.Attributes.Any(x => property.MatchesName(x.Identifier)))
                    continue;

                var info = PropertyCompletionInfo.Get(graphNode.Component, property, graphNode.State);

                foreach (var name in property.Aliases.Prepend(property.Name))
                {
                    items.Add(new CompletionItem()
                    {
                        Label = name,
                        Kind = CompletionItemKind.Property,
                        SortText = PartialIdentifier,
                        InsertText = $"{name}{info.AutoFill}",
                        InsertTextFormat = info.HasAutoFill
                            ? InsertTextFormat.Snippet
                            : InsertTextFormat.PlainText,
                        Detail = info.Details,
                        Documentation = info.Description is null ? null : new StringOrMarkupContent(new MarkupContent()
                        {
                            Kind = MarkupKind.Markdown,
                            Value = info.Description
                        })
                    });
                }
            }

            return new CompletionList(
                isIncomplete: false,
                items: items
            );
        }
    }

    public sealed record ComponentNames(
        CXComponentGraph Graph,
        int Position,
        CXElement Element,
        string? PartialIdentifier
    ) : CompletionResult(Graph, Position)
    {
        public override CompletionList ToCompletionList(ILogger logger)
        {
            Graph.TryLookupGraphNodeContainingSyntax(Element, out var encapsulatingGraphNode);

            var currentGraphNode = ReferenceEquals(encapsulatingGraphNode?.State.CXNode, Element)
                ? encapsulatingGraphNode
                : null;

            GraphNode? parentGraphNode;

            if (currentGraphNode is not null)
            {
                parentGraphNode = currentGraphNode.Parent;
            }
            else
            {
                Graph.TryLookupGraphNodeContainingSyntax(Element.FirstAncestorOfTypeOrDefault<CXElement>(), out parentGraphNode);
            }
            
            // having a valid graph node which isn't dynamic indicates that the identifier is a valid component, we
            // don't suggest anything
            if (
                currentGraphNode is not null &&
                currentGraphNode.Component is not IDynamicComponentNode
            ) return EmptyCompletionList;

            var items = new List<CompletionItem>();

            foreach (var (name, component) in ComponentNode.AccessibleComponents)
            {
                if(!ComponentValidityMap.IsValidHierarchy(parentGraphNode, component))
                    continue;
                
                var insertText = new StringBuilder(name);

                if (Element.OpeningTag.EndToken.IsMissing)
                {
                    if (component.IsParentOfOtherComponents)
                        insertText.Append(">$0</").Append(name).Append('>');
                    else
                        insertText.Append("$0/>");
                }
                else if (!component.IsParentOfOtherComponents &&
                         Element.OpeningTag.EndToken.Kind is CXTokenKind.GreaterThan)
                    insertText.Append("$0/");

                var documentation = Documentation.GetDescriptionOfComponent(component);

                items.Add(new CompletionItem()
                {
                    Label = name,
                    SortText = PartialIdentifier,
                    Kind = CompletionItemKind.Class,
                    InsertText = insertText.ToString(),
                    InsertTextFormat = InsertTextFormat.Snippet,
                    Detail = "Built-in component",
                    Documentation = documentation is not null
                        ? new StringOrMarkupContent(new MarkupContent()
                        {
                            Kind = MarkupKind.Markdown,
                            Value = documentation
                        })
                        : null
                });
            }

            return new(isIncomplete: false, items: items);
        }
    }
}