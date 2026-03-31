using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using ComponentDesigner.Nodes;
using ComponentDesigner.Nodes.TextControls;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public sealed partial class CXComponentGraph : IEquatable<CXComponentGraph>
{
    public IReadOnlyList<GraphNode> RootNodes => _tree.RootNodes;
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public CXDocument Document { get; }
    public ICXModel CX { get; }
    public IGraphOptions Options { get; }
    public IComponentImplementation Implementation { get; }

    public IReadOnlyList<GraphNode> Nodes => _tree.Nodes;

    private readonly IReadOnlyList<Diagnostic> _diagnostics;
    private readonly IReadOnlyList<Diagnostic>? _updateDiagnostics;

    private Dictionary<ICXNode, GraphNode>? _syntaxNodeToGraphNodeLookupTable;

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

    public bool TryLookupGraphNodeRepresentingSyntax(
        ICXNode? syntaxNode,
        [MaybeNullWhen(false)] out GraphNode graphNode
    ) => TryLookupGraphNodeContainingSyntax(syntaxNode, out graphNode) &&
         ReferenceEquals(syntaxNode, graphNode.State.CXNode);

    public bool TryLookupGraphNodeContainingSyntax(ICXNode? syntaxNode, [MaybeNullWhen(false)] out GraphNode graphNode)
    {
        if (syntaxNode is null)
        {
            graphNode = null;
            return false;
        }

        lock (_tree)
        {
            _syntaxNodeToGraphNodeLookupTable ??= BuildLookupTable(Nodes);
        }

        var current = syntaxNode;

        while (current is not null and not CXDocument)
        {
            if (_syntaxNodeToGraphNodeLookupTable.TryGetValue(current, out graphNode))
                return true;

            current = current.Parent;
        }

        graphNode = null;
        return false;
    }

    private static Dictionary<ICXNode, GraphNode> BuildLookupTable(IReadOnlyList<GraphNode> nodes)
    {
        if (nodes.Count is 0) return [];

        var result = new Dictionary<ICXNode, GraphNode>();

        foreach (var graphNode in nodes)
        {
            if (graphNode.State.CXNode is null) continue;

            result[graphNode.State.CXNode] = graphNode;
        }

        return result;
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

    private IReadOnlyList<Diagnostic>? _validationDiagnostics;
    private bool _validationHasErrors;

    public IReadOnlyList<Diagnostic> Validate(
        ICompilationProvider compilationProvider,
        CancellationToken cancellationToken = default
    ) => Validate(compilationProvider, out _, cancellationToken);

    public IReadOnlyList<Diagnostic> Validate(
        ICompilationProvider compilationProvider,
        out bool hasErrors,
        CancellationToken cancellationToken = default
    )
    {
        if (_validationDiagnostics is not null)
        {
            hasErrors = _validationHasErrors;
            return _validationDiagnostics;
        }

        using var bag = PooledDiagnosticBag.Get(Diagnostics);
        var context = new ComponentValidationContext(this, compilationProvider);

        for (var i = 0; i < _tree.Count; i++)
        {
            var node = _tree[i];

            if(!node.ComponentInitializationProducedDiagnostics)
                node.Component.Validate(context, node.State, bag, cancellationToken);
        }

        hasErrors = _validationHasErrors = bag.HasErrors;
        return _validationDiagnostics = bag.ToCollection();
    }

    public Result<string> Emit(ICompilationProvider compilationProvider, CancellationToken cancellationToken = default)
    {
        var validation = Validate(compilationProvider, out var hasErrors, cancellationToken);

        if (hasErrors) return new(validation);

        return Implementation
            .Renderer
            .RenderComponents(
                this,
                new ComponentEmitContext(this, compilationProvider),
                cancellationToken
            )
            .PrefaceDiagnostics(validation);
    }
    /*
     * var component = "";
     *
     * <container>
     *      {component}
     * </container>
     * 
     */
}