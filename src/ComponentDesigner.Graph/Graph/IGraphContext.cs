using ComponentDesigner.Nodes;

namespace ComponentDesigner;

public interface IGraphContext : IComponentContext
{
    IComponentRenderer Renderer { get; }
}