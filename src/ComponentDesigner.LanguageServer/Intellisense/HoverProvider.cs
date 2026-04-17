using ComponentDesigner;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;
using Discord.ComponentDesigner.LanguageServer.CX;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Discord.ComponentDesigner.LanguageServer;

public static class HoverProvider
{
    public static Hover? Get(CXComponentGraph graph, int position)
    {
        var syntaxNode = SyntaxPosition.Get(graph.Document, Math.Max(0, position - 1));

        while (syntaxNode is not null)
        {
            switch (syntaxNode)
            {
                case CXAttribute attribute:
                {
                    if (!graph.TryLookupGraphNodeContainingSyntax(attribute, out var graphNode))
                        return null;

                    if (attribute.IdentifierToken.TextSpan.IntersectsWith(position))
                    {
                        if (
                            !ComponentPropertyInfo
                                .Get(graphNode.Component, LanguageServerComponentImplementation.Instance)
                                .TryGet(attribute.Identifier, out var property)
                        ) return null;
                        
                        var description = Documentation
                            .GetDescriptionOfProperty(graphNode.Component, property, graphNode.State);

                        return description is null
                            ? null
                            : new Hover()
                            {
                                Range = ComponentDocument
                                    .GetRange(
                                        attribute.Source!,
                                        attribute.IdentifierToken.TextSpan
                                    ),
                                Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
                                {
                                    Kind = MarkupKind.Markdown,
                                    Value = description
                                })
                            };
                    }

                    break;
                }

                case CXIdentifier identifier:
                {
                    if (!graph.TryLookupGraphNodeContainingSyntax(identifier, out var graphNode))
                        return null;

                    var description = Documentation
                        .GetDescriptionOfComponent(graphNode.Component, graphNode.State);

                    return description is null
                        ? null
                        : new Hover()
                        {
                            Range = ComponentDocument.GetRange(identifier.Source!, identifier.TextSpan),
                            Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
                            {
                                Kind = MarkupKind.Markdown,
                                Value = description
                            })
                        };
                }

                    break;
            }

            syntaxNode = syntaxNode.Parent;
        }

        return null;
    }
}