using System.Buffers;
using ComponentDesigner.Nodes;
using ComponentDesigner.Nodes.TextControls;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public sealed class CXComponentGraph : IEquatable<CXComponentGraph>
{
    public IReadOnlyList<GraphNode> RootNodes => _tree.RootNodes;
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public CXDocument Document { get; }
    public ICXModel CX { get; }
    public IGraphOptions Options { get; }
    public IComponentImplementation Implementation { get; }

    private readonly IReadOnlyList<Diagnostic> _diagnostics;
    private readonly IReadOnlyList<Diagnostic>? _updateDiagnostics;

    private readonly CXComponentTree _tree;

    private CXComponentGraph(
        CXDocument document,
        CXComponentTree tree,
        IReadOnlyList<Diagnostic> diagnostics,
        ICXModel cx,
        IGraphOptions options,
        IComponentImplementation implementation,
        IReadOnlyList<Diagnostic>? updateDiagnostics = null
    )
    {
        _tree = tree;
        Document = document;
        Diagnostics = updateDiagnostics is not null ? [..diagnostics, ..updateDiagnostics] : diagnostics;
        _diagnostics = diagnostics;
        _updateDiagnostics = updateDiagnostics;
        Options = options;
        Implementation = implementation;
        CX = cx;
    }

    private CXComponentGraph(
        CXDocument document,
        CXComponentTree tree,
        IReadOnlyList<Diagnostic> diagnostics,
        GraphParameters parameters,
        IReadOnlyList<Diagnostic>? updateDiagnostics = null
    ) : this(
        document, tree, diagnostics, parameters.CX, parameters.Options, parameters.Implementation,
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
        CancellationToken cancellationToken = default
    )
    {
        var reader = new CXSourceReader(
            CXSourceText.From(parameters.CX.Syntax),
            parameters.CX.Interpolations.Select(NormalizeInterpolatedSpanToStartOfCX).ToArray(),
            parameters.CX.QuoteCount
        );

        var document = CXParser.Parse(reader, cancellationToken);

        return Create(parameters, document, cancellationToken);

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
                CXComponentTree.Empty,
                parserDiagnostics,
                parameters
            );
        }

        using var diagnostics = PooledDiagnosticBag.Get(parserDiagnostics);

        var context = new GraphInitializationContext(
            document,
            parameters.CX,
            parameters.Options,
            parameters.Implementation,
            parameters.CompilationProvider,
            diagnostics
        );

        CreateNodes(document.RootNodes, null, context, token);

        return new CXComponentGraph(
            document,
            context.Tree,
            diagnostics.ToCollection(),
            parameters
        );
    }

    internal static void CreateNodes(
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
                    var autoTextDisplayGraphNode = context.Tree.Push(
                        AutoTextDisplayComponentNode.Instance,
                        parent: parent
                    );

                    autoTextDisplayGraphNode.State = new TextDisplayState(
                        autoTextDisplayGraphNode,
                        null
                    );

                    var textControlGraphNode = context.Tree.Push(
                        TextControlNode.Instance,
                        parent: autoTextDisplayGraphNode
                    );

                    textControlGraphNode.State = new TextControlState(
                        textControlGraphNode,
                        null,
                        result
                    );
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

            CreateNodes(node, parent, context, cancellationToken);
        }
    }


    internal static void CreateNodes(
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
                CreateElementNodes(element, parent, context, cancellationToken);
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
        ICXNode cxNode,
        IInterpolationInfo info,
        GraphNode? parent,
        GraphInitializationContext context,
        CancellationToken cancellationToken = default
    )
    {
        if (!context.ComponentTypingProvider.IsValidComponentType(context, info.Symbol, cancellationToken))
        {
            // TODO: diagnostic can be improved to include type info etc
            context.Diagnostics.Add(
                cxNode.Report(
                    Diagnostic.UnsupportedSyntaxKindForGraphNode(cxNode)
                )
            );
            return;
        }

        var graphNode = context.Tree.Push(
            ComponentNode.GetNode<InterpolationComponentNode>(),
            parent: parent
        );

        var state = graphNode.Component.Initialize(
            new(cxNode, graphNode, context),
            context.Diagnostics,
            cancellationToken
        );

        if (state is null) return;

        graphNode.State = state;
    }

    internal static void CreateElementNodes(
        CXElement element,
        GraphNode? parent,
        GraphInitializationContext context,
        CancellationToken cancellationToken = default
    )
    {
        if (element.IsFragment)
        {
            foreach (var child in element.Children)
                CreateNodes(child, parent, context, cancellationToken);

            return;
        }

        if (!ComponentNode.TryGetNode(element.Identifier, out var componentNode))
        {
            ResolveUnknownElement(element, context, ref componentNode, cancellationToken);
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
            context
        );

        componentNode.RegisterGraphNode(initializationContext, cancellationToken);
    }

    private static void ResolveUnknownElement(
        CXElement element,
        GraphInitializationContext context,
        ref IComponentNode? result,
        CancellationToken cancellationToken = default
    )
    {
        // for now, just assume it to be a functional component
        result = FunctionalComponentNode.Instance;
    }

    public static GraphNode? CreateFromInitializationRequest(
        GraphNodeInitializationRequest request,
        GraphInitializationContext context,
        CancellationToken cancellationToken = default
    )
    {
        var node = context.Tree.Push(
            request.Component,
            parent: request.Parent
        );

        // map attribute nodes first
        if (request.CXNode is CXElement { OpeningTag.Attributes: { Count: > 0 } attributes })
        {
            foreach (var attribute in attributes)
            {
                if (attribute.Value is not CXValue.Element nestedElement) continue;

                CreateNodes(nestedElement.Value, node, context, cancellationToken);
            }
        }

        // then do children
        if (request.Children?.Count > 0)
        {
            CreateNodes(request.Children, node, context, cancellationToken);
        }

        var initContext = new ComponentNodeInitializationContext(
            request.CXNode,
            node,
            context
        );

        var state = node.Component.Initialize(initContext, context.Diagnostics, cancellationToken);

        if (state is null) return null;

        node.State = state;

        return node;
    }

    #endregion

    public CXComponentGraph UpdateDependencies(
        ICompilationProvider compilationProvider,
        CancellationToken cancellationToken
    )
    {
        if (!_tree.HasExternalDependencies) return this;

        var context = new GraphUpdateContext(
            CX,
            Options,
            Implementation,
            compilationProvider
        );

        using var diagnostics = PooledDiagnosticBag.Get();

        var updatedStates = ArrayPool<ComponentState?>.Shared.Rent(_tree.Count);
        var hasUpdatedState = false;

        for (var i = 0; i < _tree.NodesWithExternalDependencies.Count; i++)
        {
            var node = _tree.NodesWithExternalDependencies[i];

            var updatedState = node.Component.UpdateState(
                node.State,
                context,
                diagnostics,
                cancellationToken
            );

            updatedStates[node.Id] = updatedState;
            hasUpdatedState |= !updatedState.Equals(node.State);
        }

        if (!hasUpdatedState)
        {
            ArrayPool<ComponentState?>.Shared.Return(updatedStates);
            return this;
        }

        var newTree = new CXComponentTree();

        for (var i = 0; i < _tree.Count; i++)
            newTree.Reuse(_tree[i], updatedStates[i]);

        return new CXComponentGraph(
            Document,
            newTree,
            _diagnostics,
            CX,
            Options,
            Implementation,
            diagnostics.ToCollection()
        );
    }

    public Result<string> Emit(ICompilationProvider compilationProvider, CancellationToken cancellationToken = default)
    {
        var context = new ComponentEmitContext(this, compilationProvider);

        return Implementation.Renderer.RenderComponents(
            this,
            context,
            cancellationToken
        );
    }
}