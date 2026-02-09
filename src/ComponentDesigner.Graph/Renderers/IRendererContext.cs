using ComponentDesigner.Nodes;

namespace ComponentDesigner;

public interface IRendererContext : IComponentContext
{
    string CreateVariable(string hint = "local_");

    Result<RenderedComponent> RenderGraphNode(
        GraphNode node,
        ComponentOptions options = default,
        CancellationToken cancellationToken = default
    );
}