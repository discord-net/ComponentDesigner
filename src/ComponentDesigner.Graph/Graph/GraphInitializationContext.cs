using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;

namespace ComponentDesigner;

public sealed record GraphInitializationContext(
    CXDocument Document,
    ICXModel CX,
    IGraphOptions Options,
    IComponentImplementation Implementation,
    ICompilationProvider CompilationProvider,
    IDiagnosticBag Diagnostics,
    CXComponentTree? Tree = null
) : IGraphContext
{
    public CXComponentTree Tree { get; } = Tree ?? new();

    public bool Equals(IComponentContext? other)
        => other is GraphInitializationContext ctx && Equals(ctx);

    public GraphNode? Push(
        GraphNodeInitializationRequest request,
        CancellationToken cancellationToken = default
    ) => CXComponentGraph.CreateFromInitializationRequest(
        request,
        this,
        cancellationToken
    );

    public IReadOnlyList<GraphNode> Push(
        GraphNode? parent,
        IReadOnlyList<ICXNode> syntaxNodes,
        CancellationToken cancellationToken
    )
    {
        var target = parent?.Children ?? Tree.RootNodes;
        var start = target.Count;

        CXComponentGraph.CreateNodes(
            syntaxNodes,
            parent,
            this,
            cancellationToken
        );

        var end = target.Count;

        if (start == end) return [];

        return [..target.Skip(start).Take(end - start)];
    }
}