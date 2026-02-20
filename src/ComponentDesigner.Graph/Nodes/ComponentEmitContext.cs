using ComponentDesigner.Parser.Util;
using ComponentDesigner.Util;

namespace ComponentDesigner.Nodes;

public sealed class ComponentEmitContext(CXComponentGraph graph) :
    IRendererContext,
    IEquatable<ComponentEmitContext>
{
    public ICXModel CX => _graph.CX;

    public GraphOptions Options => _graph.Options;

    public IComponentImplementation Implementation => _graph.Implementation;

    private readonly CXComponentGraph _graph = graph;

    private Dictionary<string, int>? _varsCount;

    public Result<RenderedComponent> RenderGraphNode(
        GraphNode node,
        ComponentOptions options = default,
        CancellationToken cancellationToken = default
    ) => node.Emit(this, options, cancellationToken);

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
           ReferenceEquals(Implementation, other.Implementation) &&
           _graph.Equals(other._graph);

    public override bool Equals(object? obj)
        => obj is ComponentEmitContext other && Equals(other);

    public override int GetHashCode()
        => Hash.Combine(_graph, Implementation);

    bool IEquatable<IComponentContext>.Equals(IComponentContext? other)
        => other is ComponentEmitContext ctx && Equals(ctx);
}