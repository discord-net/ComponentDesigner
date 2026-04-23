using ComponentDesigner.Parser.Util;
using ComponentDesigner.Util;

namespace ComponentDesigner.Nodes;

public sealed class ComponentRenderingContext<TFinal, TRender>(
    CXComponentGraph graph,
    ICompilationProvider compilationProvider,
    IComponentRenderer<TFinal, TRender> renderer
) : IRenderContext<TRender>
{
    public CXComponentGraph Graph { get; } = graph;
    public IComponentRenderer<TFinal, TRender> Renderer { get; } = renderer;
    public ICompilationProvider CompilationProvider { get; } = compilationProvider;
    
    public IComponentImplementation Implementation => Graph.Implementation;
    public ICXModel CX => Graph.CX;
    public IGraphOptions Options => Graph.Options;

    IComponentRenderer<TRender> IRenderContext<TRender>.Renderer => Renderer;
}

// public sealed class ComponentEmitContext(
//     CXComponentGraph graph,
//     ICompilationProvider compilationProvider
// ) :
//     IRendererContext,
//     IEquatable<ComponentEmitContext>
// {
//     public ICXModel CX => _graph.CX;
//
//     public IGraphOptions Options => _graph.Options;
//
//     public IComponentImplementation Implementation => _graph.Implementation;
//     public ICompilationProvider CompilationProvider { get; } = compilationProvider;
//
//     private readonly CXComponentGraph _graph = graph;
//
//     private Dictionary<string, int>? _varsCount;
//
//     public Result<RenderedComponent> RenderGraphNode(
//         GraphNode node,
//         ComponentOptions options = default,
//         CancellationToken cancellationToken = default
//     ) => node.Render(this, options, cancellationToken);
//
//     public string CreateVariable(string hint = "local_")
//     {
//         _varsCount ??= [];
//
//         if (!_varsCount.TryGetValue(hint, out var count))
//             _varsCount[hint] = 1;
//         else
//             _varsCount[hint] = count + 1;
//
//         return $"{hint}{count}";
//     }
//
//     public bool Equals(ComponentEmitContext? other)
//         => other is not null &&
//            ReferenceEquals(Implementation, other.Implementation) &&
//            _graph.Equals(other._graph);
//
//     public override bool Equals(object? obj)
//         => obj is ComponentEmitContext other && Equals(other);
//
//     public override int GetHashCode()
//         => Hash.Combine(_graph, Implementation);
//
//     bool IEquatable<IComponentContext>.Equals(IComponentContext? other)
//         => other is ComponentEmitContext ctx && Equals(ctx);
// }