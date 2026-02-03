using Discord.CX.Nodes;
using Discord.CX.Nodes.Text;
using Discord.CX.Parser;
using Discord.CX.Util;

namespace Discord.CX;

public sealed class CXGraph : IEquatable<CXGraph>
{
    public IReadOnlyList<GraphNode> RootNodes { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public CXDocument Document { get; }
    public ICXModel CX { get; }
    public GraphOptions Options { get; }

    private CXGraph(
        CXDocument document,
        IReadOnlyList<GraphNode> rootNodes,
        IReadOnlyList<Diagnostic> diagnostics,
        ICXModel cx,
        GraphOptions options,
        IReadOnlyList<Diagnostic>? updateDiagnostics = null
    )
    {
        Document = document;
        RootNodes = rootNodes;
        Diagnostics = updateDiagnostics is not null ? [..diagnostics, ..updateDiagnostics] : diagnostics;
        Options = options;
        CX = cx;
    }

    public CXGraph(
        CXDocument document,
        IReadOnlyList<GraphNode> rootNodes,
        IReadOnlyList<Diagnostic> diagnostics,
        CreateGraphParameters parameters,
        IReadOnlyList<Diagnostic>? updateDiagnostics = null
    ) : this(document, rootNodes, diagnostics, parameters.CX, parameters.Options, updateDiagnostics)
    {
    }

    public bool Equals(CXGraph? other)
    {
        if (other is null) return false;

        if (ReferenceEquals(this, other)) return true;

        return
            Document.Equals(other.Document) &&
            RootNodes.SequenceEqual(other.RootNodes) &&
            Diagnostics.SequenceEqual(other.Diagnostics) &&
            CX.Equals(other.CX) &&
            Options.Equals(other.Options);
    }

    public override bool Equals(object? obj)
        => obj is CXGraph other && Equals(other);

    public override int GetHashCode()
        => Hash.Combine(
            Document, RootNodes.Aggregate(0, Hash.Combine), Diagnostics.Aggregate(0, Hash.Combine), CX, Options
        );

    public static CXGraph Create(
        CreateGraphParameters parameters,
        CancellationToken token = default
    )
    {
        var reader = new CXSourceReader(
            CXSourceText.From(parameters.CX.Syntax),
            parameters.CX.Interpolations.Select(x => x.TextSpan).ToArray(),
            parameters.CX.QuoteCount
        );

        var document = CXParser.Parse(reader, token);

        return Create(parameters, document, token);
    }

    public static CXGraph Create(
        CreateGraphParameters parameters,
        CXDocument document,
        CancellationToken token = default
    )
    {
        var parserDiagnostics = document
            .AllDiagnostics
            .Select(x => x.ToNormalDiagnostic());

        if (document.HasErrors)
        {
            return new CXGraph(
                document,
                [],
                [..parserDiagnostics],
                parameters
            );
        }

        var diagnostics = new List<Diagnostic>(parserDiagnostics);

        var rootNodes = new List<GraphNode>();

        var context = new GraphInitializationContext(
            document,
            parameters.CX,
            parameters.CompilationProvider,
            parameters.Options,
            diagnostics,
            parameters.Renderer
        );

        CreateNodes(rootNodes, document.RootNodes, null, context, token);

        return new CXGraph(
            document,
            rootNodes,
            diagnostics,
            parameters
        );
    }

    private static void CreateNodes(
        IList<GraphNode> results,
        IReadOnlyList<CXNode> nodes,
        GraphNode? parent,
        GraphInitializationContext context,
        CancellationToken token = default
    )
    {
        using var enumerator = GraphNodeEnumerator.GetNext(nodes).GetEnumerator();

        while (enumerator.MoveNext())
        {
            var node = enumerator.Current;
            
            if (
                !context.IsInterpolatedComponent(node) &&
                TextControlElement.TryCreate(
                    context,
                    enumerator,
                    context.Diagnostics,
                    out var result,
                    out var enumeratorHasMore,
                    token
                )
            )
            {
                if (context.Options.AllowAutoTextDisplays)
                {
                    var graphNode = new GraphNode(
                        AutoTextDisplayComponentNode.Instance
                    );

                    graphNode.State = new TextDisplayState(
                        graphNode,
                        null,
                        result
                    );
                    
                    results.Add(graphNode);
                }
                else
                {
                    context.Diagnostics.Add(
                        result.TextSpan.Report(
                            Diagnostic.FeatureAutoTextDisplaysDisabled
                        )
                    );
                }

                // if the text control consumed all the nodes, return out
                if (!enumeratorHasMore) return;

                node = enumerator.Current;
            }
            
            CreateNodes(results, node, parent, context, token);
        }
    }


    private static void CreateNodes(
        IList<GraphNode> results,
        ICXNode? cxNode,
        GraphNode? parent,
        GraphInitializationContext context,
        CancellationToken token = default
    )
    { 
        switch (cxNode)
        {
            case CXValue.Interpolation interpolation:
                CreateInterpolationNodes(
                    results,
                    interpolation,
                    context.CX.Interpolations[interpolation.InterpolationIndex],
                    parent,
                    context, token);
                return;

            case CXValue.Multipart multipart:
            {
                // TODO: handle text control vs interpolation
                return;
            }

            case CXElement element:
                CreateElementNodes(results, element, parent, context, token);
                return;

            default:
                if (cxNode is not null)
                {
                    context.Diagnostics.Add(
                        cxNode.Report(
                            Diagnostic.UnsupportedSyntaxKindForGraphNode(cxNode)
                        )
                    );
                }
                return;
        }
    }

    private static void CreateInterpolationNodes(
        IList<GraphNode> results,
        ICXNode cxNode,
        IInterpolationInfo info,
        GraphNode? parent,
        GraphInitializationContext context,
        CancellationToken token = default
    )
    {
        
    }

    private static void CreateElementNodes(
        IList<GraphNode> results,
        CXElement element,
        GraphNode? parent,
        GraphInitializationContext context,
        CancellationToken token = default
    )
    {
        // remap fragments
        if (element.IsFragment)
        {
            foreach (var child in element.Children)
            {
                CreateNodes(results, child, parent, context, token);
            }

            return;
        }

        if (!ComponentNode.TryGetNode(element.Identifier, out var componentNode))
        {
            ResolveUnknownElement(element, context, ref componentNode, token);
        }

        if (componentNode is null)
        {
            context.Diagnostics.Add(
                element.Report(Diagnostic.UnknownElement(element.Identifier))
            );
            return;
        }

        var initializationContext = new ComponentGraphInitializationContext(
            parent,
            element,
            context,
            results
        );

        componentNode.RegisterGraphNode(initializationContext, token);
    }

    private static void ResolveUnknownElement(
        CXElement element,
        GraphInitializationContext context,
        ref IComponentNode? result,
        CancellationToken token = default
    )
    {
        // TODO: try resolve custom components
    }

    public static GraphNode? CreateFromInitializationRequest(
        GraphNodeInitializationRequest request,
        GraphInitializationContext context,
        CancellationToken token = default
    )
    {
        var node = new GraphNode(
            request.Component,
            parent: request.Parent
        );

        var initContext = new ComponentNodeInitializationContext(
            request.CXNode,
            node,
            context
        );

        var state = node.Component.Initialize(initContext, context.Diagnostics);

        if (state is null) return null;

        node.State = state;

        if (state.CXNode is CXElement { OpeningTag.Attributes: { Count: > 0 } attributes })
        {
            foreach (var attribute in attributes)
            {
                if (attribute.Value is not CXValue.Element nestedElement) continue;

                CreateNodes(node.Attributes, nestedElement.Value, node, context, token);
            }
        }

        if (request.Children?.Count > 0)
        {
            CreateNodes(node.Children, request.Children, node, context, token);
        }

        return node;
    }
}