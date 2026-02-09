using ComponentDesigner.Parser.Util;
using ComponentDesigner.Util;

namespace ComponentDesigner.Nodes;

public sealed class ComponentEmitContext :
    IRendererContext,
    IEquatable<ComponentEmitContext>
{
    public ICompilationProvider CompilationProvider { get; }

    public ICXModel CX => _graph.CX;

    public GraphOptions Options => _graph.Options;

    public IComponentRenderer Renderer => _graph.Renderer;

    private readonly CXComponentGraph _graph;

    private Dictionary<string, int>? _varsCount;

    public ComponentEmitContext(CXComponentGraph graph, ICompilationProvider compilationProvider)
    {
        CompilationProvider = compilationProvider;
        _graph = graph;
    }

    public Result<RenderedComponent> RenderGraphNode(
        GraphNode node,
        ComponentOptions options = default,
        CancellationToken cancellationToken = default
    )  => node.Emit(this, options, cancellationToken);

    public string CreateVariable(string hint = "local_")
    {
        _varsCount ??= [];

        if (!_varsCount.TryGetValue(hint, out var count))
            _varsCount[hint] = 1;
        else
            _varsCount[hint] = count + 1;

        return $"{hint}{count}";
    }

    public bool Equals(ComponentEmitContext? other)
        => other is not null &&
           ReferenceEquals(Renderer, other.Renderer) &&
           _graph.Equals(other._graph);

    public override bool Equals(object? obj)
        => obj is ComponentEmitContext other && Equals(other);

    public override int GetHashCode()
        => Hash.Combine(_graph, Renderer);

    bool IEquatable<IComponentContext>.Equals(IComponentContext? other)
        => other is ComponentEmitContext ctx && Equals(ctx);
}