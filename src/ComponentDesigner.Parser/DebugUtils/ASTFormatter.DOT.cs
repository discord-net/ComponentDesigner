using System;
using System.Collections.Generic;
using System.Linq;

namespace ComponentDesigner.Parser;

partial class ASTFormatter
{
    public static string ToDOTFormat(this ICXNode root)
    {
        var flat = root.Walk().ToArray();
        var tokens = flat.OfType<CXToken>().ToArray();

        var parts = new List<string>()
        {
            "rankdir=TB",
            "graph [splines=ortho, ordering=out]"
        };

        parts.Add(
            $$"""
              node [shape = box];
              {
                rank=same
                {{string.Join(
                    $"{Environment.NewLine}  ",
                    tokens.Select(x =>
                        $"{Array.IndexOf(flat, x)} [label=<<b>{x.Kind}</b><br/>{HtmlClean(x.RawValue)}>]"
                    )
                )}}
                
                {{string.Join("--", tokens.Select(x => Array.IndexOf(flat, x)))}} [style=invis]
              }
              """
        );

        var nodes = new List<string>();
        var edges = new List<string>();
        
        for (var i = 0; i < flat.Length; i++)
        {
            var node = flat[i];

            if (node is ICXCollection { Count: 0 }) continue;

            if (node is CXNode)
            {
                // add the node
                nodes.Add($"{i} [label=\"{node.GetType().Name}\"]");
            }

            switch (node)
            {
                case CXDocument { RootNodes.Count: not 0 } cxDoc:
                {
                    edges.Add($"{i} -- {{{string.Join(" ", cxDoc.RootNodes.Select(x => Array.IndexOf(flat, x)))}}}");

                    break;
                }
                case CXElement cxElement:
                {
                    AddEdges(
                        i,
                        cxElement.OpeningTag.StartToken,
                        cxElement.OpeningTag.Identifier,
                        cxElement.OpeningTag.Attributes,
                        cxElement.OpeningTag.EndToken,
                        cxElement.Children,
                        cxElement.ClosingTag.StartToken,
                        cxElement.ClosingTag.IdentifierToken,
                        cxElement.ClosingTag.EndToken
                    );

                    break;
                }
                case CXAttribute cxAttribute:
                    AddEdges(
                        i,
                        cxAttribute.IdentifierToken,
                        cxAttribute.EqualsToken,
                        cxAttribute.Value
                    );
                    break;
                case CXValue.Element element:
                    AddEdges(
                        i,
                        element.OpenParenthesis,
                        element.Value,
                        element.CloseParenthesis
                    );
                    break;
                case CXValue.Interpolation interpolation:
                    AddEdges(i, interpolation.Token);
                    break;
                case CXValue.StringLiteral stringLiteral:
                    AddEdges(i, stringLiteral.StartToken, stringLiteral.Tokens, stringLiteral.EndToken);
                    break;
                case CXValue.Multipart multipart:
                    AddEdges(i, multipart.Tokens);
                    break;
                case CXValue.Scalar scalar:
                    AddEdges(i, scalar.Token);
                    break;
                case ICXCollection{Count:not 0} cxCollection:
                    edges.Add($"{i} -- {{{string.Join(" ", cxCollection.ToList().Select(x => Array.IndexOf(flat, x)))}}}");
                    break;
            }
        }
        
        parts.AddRange(nodes);
        parts.AddRange(edges);

        return
            $$"""
              graph syntax {
                {{string.Join(Environment.NewLine, parts).Replace(Environment.NewLine, $"{Environment.NewLine}  ")}}
              }
              """;

        void AddEdges(int index, params IEnumerable<ICXNode?> nodes)
        {
            var targets = nodes
                .Where(x => x is not null and not ICXCollection { Count: 0 })
                .Select(x => Array.IndexOf(flat, x))
                .Where(x => x is not -1)
                .ToArray();

            if (targets.Length is 0) return;

            edges.Add($"{index} -- {{{string.Join(" ", targets)}}}");

            // if (nodes.Length is 0) return;
            //
            // var targetIndex = Array.IndexOf(flat, node);
            //
            // if (targetIndex is -1) return;
            //
            // var part = $"{targetIndex} -- {index}";
            //
            // if (name is not null) part += $" [label=\"{name}\"]";
            //
            // parts.Add(part);
        }

        static string HtmlClean(string str)
            => str.Replace("<", "&lt;").Replace(">", "&gt;");
    }
}