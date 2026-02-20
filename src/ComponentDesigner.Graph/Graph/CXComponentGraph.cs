using ComponentDesigner.Nodes;
using ComponentDesigner.Nodes.TextControls;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public sealed class CXComponentGraph : IEquatable<CXComponentGraph>
{
    public IReadOnlyList<GraphNode> RootNodes { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public CXDocument Document { get; }
    public ICXModel CX { get; }
    public GraphOptions Options { get; }
    public IComponentImplementation Implementation { get; }

    private readonly IReadOnlyList<Diagnostic> _diagnostics;
    private readonly IReadOnlyList<Diagnostic>? _updateDiagnostics;


    private CXComponentGraph(
        CXDocument document,
        IReadOnlyList<GraphNode> rootNodes,
        IReadOnlyList<Diagnostic> diagnostics,
        ICXModel cx,
        GraphOptions options,
        IComponentImplementation implementation,
        IReadOnlyList<Diagnostic>? updateDiagnostics = null
    )
    {
        Document = document;
        RootNodes = rootNodes;
        Diagnostics = updateDiagnostics is not null ? [..diagnostics, ..updateDiagnostics] : diagnostics;
        _diagnostics = diagnostics;
        _updateDiagnostics = updateDiagnostics;
        Options = options;
        Implementation = implementation;
        CX = cx;
    }

    public CXComponentGraph(
        CXDocument document,
        IReadOnlyList<GraphNode> rootNodes,
        IReadOnlyList<Diagnostic> diagnostics,
        GraphParameters parameters,
        IReadOnlyList<Diagnostic>? updateDiagnostics = null
    ) : this(
        document, rootNodes, diagnostics, parameters.CX, parameters.Options, parameters.Implementation,
        updateDiagnostics
    )
    {
    }

    public bool Equals(CXComponentGraph? other)
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
        => obj is CXComponentGraph other && Equals(other);

    public override int GetHashCode()
        => Hash.Combine(
            Document, RootNodes.Aggregate(0, Hash.Combine), Diagnostics.Aggregate(0, Hash.Combine), CX, Options
        );

    public static bool IsLikelyComponent(
        IComponentContext context,
        ICXNode? cxNode,
        CancellationToken cancellationToken = default
    )
    {
        if (cxNode is null) return false;

        if (context.IsInterpolatedComponent(cxNode, cancellationToken)) return true;

        return cxNode is CXElement;
    }

    #region Create

    public static CXComponentGraph Create(
        GraphParameters parameters,
        CancellationToken token = default
    )
    {
        var reader = new CXSourceReader(
            CXSourceText.From(parameters.CX.Syntax),
            parameters.CX.Interpolations.Select(NormalizeInterpolatedSpanToStartOfCX).ToArray(),
            parameters.CX.QuoteCount
        );

        var document = CXParser.Parse(reader, token);

        return Create(parameters, document, token);

        CXTextSpan NormalizeInterpolatedSpanToStartOfCX(IInterpolationInfo info)
        {
            return new CXTextSpan(
                info.TextSpan.Start - parameters.CX.Location.TextSpan.Start,
                info.TextSpan.Length
            );
        }
    }

    public static CXComponentGraph Create(
        GraphParameters parameters,
        CXDocument document,
        CancellationToken token = default
    )
    {
        var parserDiagnostics = document
            .AllDiagnostics
            .Select(x => x.ToNormalDiagnostic())
            .ToArray();

        if (document.HasErrors)
        {
            return new CXComponentGraph(
                document,
                [],
                parserDiagnostics,
                parameters
            );
        }

        using var diagnostics = PooledDiagnosticBag.Get(parserDiagnostics);

        var rootNodes = new List<GraphNode>();

        var context = new GraphInitializationContext(
            document,
            parameters.CX,
            parameters.Options,
            parameters.Implementation,
            diagnostics
        );

        CreateNodes(rootNodes, document.RootNodes, null, context, token);

        return new CXComponentGraph(
            document,
            rootNodes,
            diagnostics.ToCollection(),
            parameters
        );
    }

    internal static void CreateNodes(
        IList<GraphNode> results,
        IReadOnlyList<ICXNode> nodes,
        GraphNode? parent,
        GraphInitializationContext context,
        CancellationToken cancellationToken = default
    )
    {
        using var enumerator = GraphNodeEnumerator.GetNext(nodes).GetEnumerator();

        while (enumerator.MoveNext())
        {
            var node = enumerator.Current;

            if (
                !context.IsInterpolatedComponent(node, cancellationToken) &&
                TextControlElement.TryCreate(
                    context,
                    enumerator,
                    context.Diagnostics,
                    out var result,
                    out var enumeratorHasMore,
                    cancellationToken
                )
            )
            {
                if (context.Options.AllowAutoTextDisplays)
                {
                    var autoTextDisplayGraphNode = new GraphNode(
                        AutoTextDisplayComponentNode.Instance
                    );

                    autoTextDisplayGraphNode.State = new TextDisplayState(
                        autoTextDisplayGraphNode,
                        null
                    );

                    var textControlGraphNode = new GraphNode(TextControlNode.Instance);

                    textControlGraphNode.State = new TextControlState(
                        textControlGraphNode,
                        null,
                        result
                    );

                    autoTextDisplayGraphNode.Children.Add(textControlGraphNode);
                    results.Add(autoTextDisplayGraphNode);
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

            CreateNodes(results, node, parent, context, cancellationToken);
        }
    }


    internal static void CreateNodes(
        IList<GraphNode> results,
        ICXNode? cxNode,
        GraphNode? parent,
        GraphInitializationContext context,
        CancellationToken cancellationToken = default
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
                    context, cancellationToken);
                return;

            case CXValue.Multipart multipart:
            {
                // TODO: handle text control vs interpolation
                return;
            }

            case CXElement element:
                CreateElementNodes(results, element, parent, context, cancellationToken);
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

    internal static void CreateInterpolationNodes(
        IList<GraphNode> results,
        ICXNode cxNode,
        IInterpolationInfo info,
        GraphNode? parent,
        GraphInitializationContext context,
        CancellationToken token = default
    )
    {
        if (!context.ComponentTypingProvider.IsValidComponentType(context, info.Symbol, token))
        {
            // TODO: diagnostic can be improved to include type info etc
            context.Diagnostics.Add(
                cxNode.Report(
                    Diagnostic.UnsupportedSyntaxKindForGraphNode(cxNode)
                )
            );
            return;
        }

        var graphNode = new GraphNode(ComponentNode.GetNode<InterpolationComponentNode>());

        var state = graphNode.Component.Initialize(
            new(cxNode, graphNode, context),
            context.Diagnostics,
            token
        );

        if (state is null) return;

        graphNode.State = state;

        results.Add(graphNode);
    }

    internal static void CreateElementNodes(
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
                element.Report(Diagnostic.UnknownComponentElement(element.Identifier))
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
        CancellationToken cancellationToken = default
    )
    {
        var node = new GraphNode(
            request.Component,
            parent: request.Parent
        );

        if (request.Children?.Count > 0)
        {
            CreateNodes(node.Children, request.Children, node, context, cancellationToken);
        }
        
        var initContext = new ComponentNodeInitializationContext(
            request.CXNode,
            node,
            context
        );

        var state = node.Component.Initialize(initContext, context.Diagnostics, cancellationToken);

        if (state is null) return null;

        node.State = state;

        if (state.CXNode is CXElement { OpeningTag.Attributes: { Count: > 0 } attributes })
        {
            foreach (var attribute in attributes)
            {
                if (attribute.Value is not CXValue.Element nestedElement) continue;

                CreateNodes(node.Attributes, nestedElement.Value, node, context, cancellationToken);
            }
        }

        return node;
    }

    #endregion

    public CXComponentGraph UpdateState(
        GraphParameters parameters,
        CancellationToken cancellationToken
    )
    {
        var context = new GraphUpdateContext(
            parameters.CX,
            parameters.Options,
            parameters.Implementation
        );

        using var diagnostics = PooledDiagnosticBag.Get();

        var shouldUpdate =
            !parameters.CX.Equals(CX) ||
            !parameters.Options.Equals(Options) ||
            !parameters.Implementation.Equals(Implementation);


        var rootNodes = new GraphNode[RootNodes.Count];

        for (var i = 0; i < RootNodes.Count; i++)
        {
            var node = RootNodes[i];
            rootNodes[i] = node.Update(context, diagnostics, cancellationToken);

            if (!shouldUpdate) shouldUpdate = node.Equals(rootNodes[i]);
        }

        shouldUpdate |= diagnostics.HasAny;

        return shouldUpdate
            ? new CXComponentGraph(
                Document,
                rootNodes,
                _diagnostics,
                parameters,
                diagnostics.ToCollection()
            )
            : this;
    }

    public Result<string> Emit(CancellationToken cancellationToken = default)
    {
        var context = new ComponentEmitContext(this);

        return Implementation.Renderer.RenderComponents(
            this,
            context,
            cancellationToken
        );
    }
}